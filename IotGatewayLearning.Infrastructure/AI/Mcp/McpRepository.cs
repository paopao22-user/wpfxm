using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IotGatewayLearning.Infrastructure.AI.Mcp
{
    public sealed class McpRepository : IMcpRepository
    {
        private readonly AppDbContext _db;

        public McpRepository(AppDbContext db) => _db = db;

        public Task<bool> ActorExistsAsync(string actor, CancellationToken ct) =>
            _db.Users.AsNoTracking()
                .AnyAsync(x => x.Username == actor && x.Status == 1, ct);

        // Device 配了全局查询过滤器 is_deleted = 0，这里只需再判断启用状态。
        public Task<bool> DeviceExistsAsync(string device, CancellationToken ct) =>
            _db.Devices.AsNoTracking()
                .AnyAsync(x => x.DeviceCode == device && x.Status == 1, ct);

        public Task<bool> AllowedAsync(
            string actor,
            string device,
            string tool,
            CancellationToken ct) =>
            _db.Set<McpGrant>().AsNoTracking()
                .AnyAsync(x => x.Actor == actor && x.DeviceCode == device && x.Tool == tool, ct);

        public async Task ChangeAsync(
            string actor,
            string device,
            string tool,
            string operatorName,
            bool grant,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // 授权表和变更表必须一起成功或一起失败：
            // 只改授权不记变更 = 审计断链；只记变更不改授权 = 授权不生效。
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (grant)
            {
                // 重复授予不新增三元组，只刷新操作员和时间。
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO ai_mcp_grants (actor, device_code, tool, granted_by, granted_at)
                    VALUES ({actor}, {device}, {tool}, {operatorName}, {now})
                    ON DUPLICATE KEY UPDATE
                        granted_by = VALUES(granted_by),
                        granted_at = VALUES(granted_at)
                    """, ct);
            }
            else
            {
                // 删除精确三元组：允许清理已停用账号或设备的遗留授权。
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM ai_mcp_grants
                    WHERE actor = {actor} AND device_code = {device} AND tool = {tool}
                    """, ct);
            }

            // 重复撤销不报错，但仍然记录一次变更，保留操作痕迹。
            _db.Set<McpGrantChange>().Add(new McpGrantChange
            {
                Operator = operatorName,
                Actor = actor,
                DeviceCode = device,
                Tool = tool,
                Action = grant ? "Grant" : "Revoke",
                OccurredAt = now
            });
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }

        public Task<List<McpGrant>> GrantsAsync(CancellationToken ct) =>
            _db.Set<McpGrant>().AsNoTracking()
                .OrderByDescending(x => x.GrantedAt)
                .Take(100)
                .ToListAsync(ct);

        public Task<List<McpGrantChange>> ChangesAsync(CancellationToken ct) =>
            _db.Set<McpGrantChange>().AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync(ct);

        public async Task AddCallAsync(McpCall call, CancellationToken ct)
        {
            // ① 先落 Started：被拒绝的越权请求也要留痕。
            _db.Set<McpCall>().Add(call);
            await _db.SaveChangesAsync(ct);
        }

        public async Task CompleteAsync(
            string id,
            string status,
            string result,
            CancellationToken ct)
        {
            // AddCallAsync 之后实体已被当前 DbContext 跟踪，这里直接改同一实例。
            var call = await _db.Set<McpCall>().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (call is null)
            {
                return;
            }

            call.Status = status;
            call.ResultJson = result;
            call.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        public Task<List<McpCall>> CallsAsync(CancellationToken ct) =>
            _db.Set<McpCall>().AsNoTracking()
                .OrderByDescending(x => x.StartedAt)
                .Take(100)
                .ToListAsync(ct);

        public Task<McpCall?> CallAsync(string id, CancellationToken ct) =>
            _db.Set<McpCall>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, ct);
    }
}
