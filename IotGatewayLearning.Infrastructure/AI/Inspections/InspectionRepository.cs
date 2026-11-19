using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace IotGatewayLearning.Infrastructure.AI.Inspections
{
    public sealed class InspectionRepository : IInspectionRepository
    {
        private readonly AppDbContext _db;

        public InspectionRepository(AppDbContext db) => _db = db;

        public Task<InspectionPlan?> PlanAsync(long id, CancellationToken ct) =>
            _db.Set<InspectionPlan>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, ct);

        public Task<List<InspectionPlan>> DueAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            return _db.Set<InspectionPlan>().AsNoTracking()
                .Where(x => x.Enabled && x.NextAt <= now)
                .OrderBy(x => x.NextAt)
                .Take(20)
                .ToListAsync(ct);
        }

        public async Task AdvanceAsync(InspectionPlan plan, CancellationToken ct)
        {
            // 只补当前窗口：从"现在"开始算下一次，不循环补历史漏掉的周期。
            var next = DateTime.UtcNow.AddSeconds(plan.IntervalSeconds);

            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_inspection_plans SET next_at = {next} WHERE id = {plan.Id}
                """, ct);

            plan.NextAt = next;
        }

        public async Task<InspectionRun?> ClaimAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // ① 租约回收：Worker 崩溃会留下 Running，超过 4 分钟统一置 Unknown。
            // 不自动重新消费模型额度——无法确认上一次调用到底有没有产生费用。
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_inspection_runs
                SET state = 'Unknown', error = '执行租约过期，请核对后重新提交新窗口'
                WHERE state = 'Running' AND lease_until < {now}
                """, ct);

            // ② 取一条可执行任务。
            var candidate = await _db.Set<InspectionRun>().AsNoTracking()
                .Where(x => (x.State == "Queued" || x.State == "Retry") && x.NextAt <= now)
                .OrderBy(x => x.NextAt)
                .ThenBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (candidate is null)
            {
                return null;
            }

            // ③ 条件抢占：影响行数必须是 1，否则说明被别的实例抢走了。
            var lease = now.AddMinutes(4);

            var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_inspection_runs
                SET state = 'Running', attempts = attempts + 1, lease_until = {lease}
                WHERE id = {candidate.Id} AND state IN ('Queued','Retry')
                """, ct);

            if (affected != 1)
            {
                return null;
            }

            candidate.State = "Running";
            candidate.Attempts += 1;
            candidate.LeaseUntil = lease;

            return candidate;
        }

        public async Task EvidenceAsync(string id, string evidence, CancellationToken ct)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_inspection_runs SET evidence_json = {evidence}
                WHERE id = {id} AND state = 'Running'
                """, ct);
        }

        public async Task FinishAsync(InspectionRun row, CancellationToken ct)
        {
            // 只更新仍在 Running 的行：
            // 如果租约已经过期被巡检置为 Unknown，这里不能把它"改回"Succeeded。
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_inspection_runs
                SET state = {row.State},
                    attempts = {row.Attempts},
                    evidence_json = {row.EvidenceJson},
                    answer = {row.Answer},
                    error = {row.Error},
                    next_at = {row.NextAt},
                    lease_until = NULL,
                    completed_at = {row.CompletedAt}
                WHERE id = {row.Id} AND state = 'Running'
                """, ct);
        }

        public async Task<InspectionPlan> AddPlanAsync(InspectionPlan plan, CancellationToken ct)
        {
            _db.Set<InspectionPlan>().Add(plan);
            await _db.SaveChangesAsync(ct);
            return plan;
        }

        public Task<List<InspectionPlan>> PlansAsync(string owner, CancellationToken ct) =>
            _db.Set<InspectionPlan>().AsNoTracking()
                .Where(x => x.Owner == owner)
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync(ct);

        public async Task<InspectionRun> EnqueueAsync(InspectionRun run, CancellationToken ct)
        {
            // 先按 MergeKey 查：同一窗口重复提交直接返回已有任务，不重复消费模型额度。
            var existing = await _db.Set<InspectionRun>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.MergeKey == run.MergeKey, ct);

            if (existing is not null)
            {
                return existing;
            }

            _db.Set<InspectionRun>().Add(run);

            try
            {
                await _db.SaveChangesAsync(ct);
                return run;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                // 并发提交由唯一索引裁决：谁先插入谁生效，后到的回读旧任务。
                _db.Entry(run).State = EntityState.Detached;

                return await _db.Set<InspectionRun>().AsNoTracking()
                    .SingleAsync(x => x.MergeKey == run.MergeKey, ct);
            }
        }

        public async Task<bool> EnableAsync(
            long id,
            string owner,
            bool enabled,
            CancellationToken ct)
        {
            // 条件更新带 Owner：既做权限校验，也避免先查后改的竞态。
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_inspection_plans SET enabled = {enabled}
                WHERE id = {id} AND owner = {owner}
                """, ct);

            return affected == 1;
        }

        public Task<List<InspectionRun>> RunsAsync(string owner, CancellationToken ct) =>
            _db.Set<InspectionRun>().AsNoTracking()
                .Where(x => x.Owner == owner)
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync(ct);

        public Task<InspectionRun?> RunAsync(string id, string owner, CancellationToken ct) =>
            _db.Set<InspectionRun>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.Owner == owner, ct);
    }
}
