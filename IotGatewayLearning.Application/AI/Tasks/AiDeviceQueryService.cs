using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IotGatewayLearning.Application.AI.Tasks
{
    /// <summary>
    /// 设备查询适配器：把现有设备 Service、采样表和告警表包装成
    /// 工具可以调用的只读查询。实现放在 Application，是为了复用
    /// IDeviceService，同时避免 Infrastructure 反向依赖 Application。
    /// </summary>
    public sealed class AiDeviceQueryService : IAiDeviceQuery
    {
        private readonly AppDbContext _db;
        private readonly IDeviceService _devices;

        public AiDeviceQueryService(AppDbContext db, IDeviceService devices)
        {
            _db = db;
            _devices = devices;
        }

        public sealed class IdRow
        {
            public long Id { get; set; }
        }

        public sealed class AlarmRow
        {
            public long Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime OccurredAt { get; set; }
        }

        // 每次工具执行都重新确认设备存在且启用，防止任务运行期间设备被停用。
        private async Task<long> RequireAsync(string device, CancellationToken ct)
        {
            var rows = await _db.Database.SqlQuery<IdRow>($"""
                SELECT id AS Id FROM devices
                WHERE device_code={device} AND is_deleted=0 AND status=1 LIMIT 1
                """).ToListAsync(ct);

            return rows.FirstOrDefault()?.Id
                ?? throw new AppException("设备不存在或已停用", 404);
        }

        public async Task<object> DeviceAsync(string device, CancellationToken ct)
        {
            var id = await RequireAsync(device, ct);
            return await _devices.GetDetailAsync(id, ct);
        }

        public async Task<object> HistoryAsync(
            string device,
            string point,
            DateTime end,
            CancellationToken ct)
        {
            await RequireAsync(device, ct);

            // 固定窗口，最多取 61 条；超过 60 条明确标记截断。
            var rows = await _db.Set<DeviceData>().AsNoTracking()
                .Where(x => x.DeviceCode == device &&
                            x.PointCode == point &&
                            x.OccurredAt >= end.AddMinutes(-1) &&
                            x.OccurredAt < end)
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.Id)
                .Take(61)
                .Select(x => new
                {
                    x.Id,
                    x.ValueDecimal,
                    x.Unit,
                    x.Quality,
                    x.OccurredAt
                })
                .ToListAsync(ct);

            return new
            {
                device,
                point,
                windowEnd = end,
                truncated = rows.Count > 60,
                samples = rows.Take(60)
            };
        }

        public async Task<object> AlarmsAsync(string device, CancellationToken ct)
        {
            await RequireAsync(device, ct);

            // 空列表只表示没有匹配项，不代表设备安全。
            return await _db.Database.SqlQuery<AlarmRow>($"""
                SELECT id AS Id, title AS Title, status_code AS Status, occurred_at AS OccurredAt
                FROM alarms WHERE device_code={device}
                  AND status_code IN ('Active','Acknowledged')
                ORDER BY id DESC LIMIT 20
                """).ToListAsync(ct);
        }
    }
}
