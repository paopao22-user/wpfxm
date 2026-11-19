using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace IotGatewayLearning.Infrastructure.Repositories.Implementations
{
    public sealed class ControlRepository : IControlRepository
    {
        private readonly AppDbContext _db;

        public ControlRepository(AppDbContext db) => _db = db;

        public Task<ControlRequest?> FindAsync(string id, CancellationToken ct) =>
            _db.Set<ControlRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, ct);

        public Task<ControlRequest?> ByKeyAsync(string key, CancellationToken ct) =>
            _db.Set<ControlRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestKey == key, ct);

        public Task<List<ControlAudit>> AuditsAsync(string id, CancellationToken ct) =>
            _db.Set<ControlAudit>().AsNoTracking()
                .Where(x => x.RequestId == id)
                .OrderBy(x => x.Id)
                .ToListAsync(ct);

        // Device 配了全局查询过滤器 is_deleted = 0，这里只判断启用状态。
        public Task<bool> DeviceExistsAsync(string code, CancellationToken ct) =>
            _db.Devices.AsNoTracking()
                .AnyAsync(x => x.DeviceCode == code && x.Status == 1, ct);

        public Task<List<ControlRequest>> ListAsync(
            string actor,
            bool review,
            CancellationToken ct)
        {
            var query = _db.Set<ControlRequest>().AsNoTracking();

            // 审批队列只看待处理；本人列表只看自己提交的。
            query = review
                ? query.Where(x => x.State == "Pending")
                : query.Where(x => x.Requester == actor);

            return query
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync(ct);
        }

        public async Task<ControlRequest> AddAsync(ControlRequest row, CancellationToken ct)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _db.Set<ControlRequest>().Add(row);

            // 申请与提交审计必须一起成功，否则审计链从第一条就断了。
            _db.Set<ControlAudit>().Add(new ControlAudit
            {
                RequestId = row.Id,
                Actor = row.Requester,
                Action = "Submit",
                Remark = row.Reason,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                // 并发重复提交：唯一索引裁决，回滚后回读先到的那条。
                await tx.RollbackAsync(ct);
                _db.ChangeTracker.Clear();

                return await ByKeyAsync(row.RequestKey, ct)
                    ?? throw new InvalidOperationException("请求键冲突但记录不可见");
            }

            await tx.CommitAsync(ct);
            return row;
        }

        public async Task<bool> DecideAsync(
            string id,
            string actor,
            bool approve,
            string remark,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // 状态前置条件全部写进 WHERE：
            // Pending、未过期、且不能审批自己的申请。
            var changed = await _db.Set<ControlRequest>()
                .Where(x => x.Id == id
                    && x.State == "Pending"
                    && x.Requester != actor
                    && x.ReviewBefore > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.State, approve ? "Approved" : "Rejected")
                    .SetProperty(x => x.Reviewer, actor)
                    .SetProperty(x => x.UpdatedAt, now)
                    .SetProperty(
                        x => x.ExecuteBefore,
                        approve ? (DateTime?)now.AddMinutes(5) : null), ct);

            if (changed != 1)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            _db.Set<ControlAudit>().Add(new ControlAudit
            {
                RequestId = id,
                Actor = actor,
                Action = approve ? "Approve" : "Reject",
                Remark = remark,
                CreatedAt = now
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return true;
        }

        public async Task<bool> CancelAsync(
            string id,
            string actor,
            string remark,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // 只有本人、且还没开始执行时才能撤销。
            var changed = await _db.Set<ControlRequest>()
                .Where(x => x.Id == id
                    && x.Requester == actor
                    && (x.State == "Pending" || x.State == "Approved"))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.State, "Canceled")
                    .SetProperty(x => x.UpdatedAt, now), ct);

            if (changed != 1)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            _db.Set<ControlAudit>().Add(new ControlAudit
            {
                RequestId = id,
                Actor = actor,
                Action = "Cancel",
                Remark = remark,
                CreatedAt = now
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return true;
        }
    }
}
