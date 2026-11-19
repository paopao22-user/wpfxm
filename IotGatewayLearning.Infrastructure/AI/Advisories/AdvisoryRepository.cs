using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI.Knowledge;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace IotGatewayLearning.Infrastructure.AI.Advisories
{
    public sealed class AdvisoryRepository : IAdvisoryRepository
    {
        private readonly AppDbContext _db;

        public AdvisoryRepository(AppDbContext db) => _db = db;

        // ==============================
        // 通知 Outbox
        // ==============================

        public async Task<AiNotification?> ClaimAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // ① Sending 超过 2 分钟视为状态未知：
            // 保留记录、不自动重发，避免第三方其实已经收到却又发一遍。
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_notification_outbox
                SET state = 'Unknown', last_error = '投递状态未知，请人工核对', updated_at = {now}
                WHERE state = 'Sending' AND updated_at < {now.AddMinutes(-2)}
                """, ct);

            // ② 取一条到期待办。
            var candidate = await _db.Set<AiNotification>().AsNoTracking()
                .Where(x => (x.State == "Pending" || x.State == "Retry") && x.NextAt <= now)
                .OrderBy(x => x.NextAt)
                .FirstOrDefaultAsync(ct);

            if (candidate is null)
            {
                return null;
            }

            // ③ 条件抢占：影响行数必须是 1。
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_notification_outbox
                SET state = 'Sending', attempts = attempts + 1, updated_at = {now}
                WHERE id = {candidate.Id} AND state IN ('Pending','Retry')
                """, ct);

            if (affected != 1)
            {
                return null;
            }

            candidate.State = "Sending";
            candidate.Attempts += 1;
            candidate.UpdatedAt = now;

            return candidate;
        }

        public async Task DeliveredAsync(
            AiNotification n,
            string state,
            string error,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // 退避：30 * Attempts 秒（30、60、90）。
            var next = now.AddSeconds(30 * n.Attempts);

            // 只更新仍在 Sending 的行，避免覆盖被 Unknown 回收的待办。
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ai_notification_outbox
                SET state = {state}, last_error = {error}, next_at = {next}, updated_at = {now}
                WHERE id = {n.Id} AND state = 'Sending'
                """, ct);

            n.State = state;
            n.LastError = error;
            n.NextAt = next;
            n.UpdatedAt = now;
        }

        public Task<List<AiNotification>> NotificationsAsync(string owner, CancellationToken ct) =>
            _db.Set<AiNotification>().AsNoTracking()
                .Join(
                    _db.Set<AiAdvisory>().AsNoTracking(),
                    n => n.AdvisoryId,
                    a => a.Id,
                    (n, a) => new { Notification = n, Advisory = a })
                .Where(x => x.Advisory.Owner == owner)
                .OrderByDescending(x => x.Notification.UpdatedAt)
                .Take(100)
                .Select(x => x.Notification)
                .ToListAsync(ct);

        // ==============================
        // 研判
        // ==============================

        public Task<AiAdvisory?> GetAsync(string id, CancellationToken ct) =>
            _db.Set<AiAdvisory>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, ct);

        public Task<AiAdvisory?> ByRunAsync(string runId, CancellationToken ct) =>
            _db.Set<AiAdvisory>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RunId == runId, ct);

        public async Task<object> EvidenceAsync(
            InspectionRun run,
            long? answerId,
            CancellationToken ct)
        {
            var knowledge = await KnowledgeAsync(run, answerId, ct);

            // 本项目没有维修记录表（文档里的 repairs 来自另一个工程的既有模块），
            // 这里固定返回空数组，并明确告诉模型"资料缺失"，而不是编造维修历史。
            var repairs = Array.Empty<object>();

            return new
            {
                run.Id,
                run.DeviceCode,
                run.EvidenceJson,
                run.Answer,
                knowledge,
                knowledgeMissing = knowledge is null,
                repairs
            };
        }

        private async Task<object?> KnowledgeAsync(
            InspectionRun run,
            long? answerId,
            CancellationToken ct)
        {
            if (answerId is not long id)
            {
                return null;
            }

            // 只能用本人的、已完成的知识库回答，别人的回答不能当证据。
            var answer = await _db.Set<KnowledgeAnswer>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.Owner == run.Owner, ct);

            if (answer is null || answer.State != "Succeeded")
            {
                throw new AppException("知识库回答不存在或不可用", 400);
            }

            var citations = JsonSerializer.Deserialize<List<KnowledgeCitation>>(answer.CitationsJson)
                ?? new List<KnowledgeCitation>();

            if (citations.Count == 0)
            {
                throw new AppException("知识库回答没有可用引用", 400);
            }

            var deviceId = await _db.Devices.AsNoTracking()
                .Where(x => x.DeviceCode == run.DeviceCode)
                .Select(x => x.Id)
                .SingleAsync(ct);

            var docIds = citations.Select(x => x.DocumentId).Distinct().ToList();

            var docs = await _db.Set<KnowledgeDocument>().AsNoTracking()
                .Where(x => docIds.Contains(x.Id))
                .ToListAsync(ct);

            foreach (var citation in citations)
            {
                var doc = docs.FirstOrDefault(x => x.Id == citation.DocumentId);

                // 引用必须指向当前设备、当前所有者、已就绪、版本一致，
                // 否则这份资料讲的可能是别的设备。
                if (doc is null
                    || doc.DeviceId != deviceId
                    || doc.Owner != run.Owner
                    || doc.State != "Ready"
                    || doc.Revision != citation.Revision)
                {
                    throw new AppException("引用不适用当前设备", 400);
                }
            }

            return new
            {
                answer.Id,
                answer.Question,
                answer.Answer
            };
        }

        public async Task<bool> StartAsync(AiAdvisory row, CancellationToken ct)
        {
            // 先落 Running 占住 RunId 唯一键：同一份证据只研判一次。
            _db.Set<AiAdvisory>().Add(row);

            try
            {
                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                _db.Entry(row).State = EntityState.Detached;
                return false;
            }
        }

        public async Task CompleteAsync(AiAdvisory row, bool notify, CancellationToken ct)
        {
            row.UpdatedAt = DateTime.UtcNow;

            if (notify)
            {
                // 研判与待办在同一个 SaveChanges 事务里：
                // 不会出现"研判已 Active 但通知待办丢了"的半成品。
                _db.Set<AiNotification>().Add(new AiNotification
                {
                    AdvisoryId = row.Id,
                    State = "Pending",
                    NextAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        public Task<List<AiAdvisory>> ListAsync(string owner, CancellationToken ct) =>
            _db.Set<AiAdvisory>().AsNoTracking()
                .Where(x => x.Owner == owner)
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync(ct);

        public async Task<bool> HandleAsync(
            string id,
            string owner,
            bool close,
            string remark,
            CancellationToken ct)
        {
            var from = close ? "Acknowledged" : "Active";
            var to = close ? "Closed" : "Acknowledged";
            var now = DateTime.UtcNow;

            // 状态写在 WHERE 里：并发点两次只有一个能成功，
            // 也天然阻止"Active 直接关闭"和"Closed 改回 Acknowledged"。
            var affected = await _db.Set<AiAdvisory>()
                .Where(x => x.Id == id && x.Owner == owner && x.State == from)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.State, to)
                    .SetProperty(x => x.Remark, remark)
                    .SetProperty(x => x.HandledBy, owner)
                    .SetProperty(x => x.UpdatedAt, now), ct);

            return affected == 1;
        }
    }
}
