using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace IotGatewayLearning.Infrastructure.Repositories.Implementations
{
    // partial：同一个仓储分在多个文件中，每个业务文件只声明自己的方法。
    // 构造函数只在这个文件定义一次，后面的查询文件复用 _db。
    public sealed partial class AiAnalysisRepository : IAiAnalysisRepository
    {
        private readonly AppDbContext _db;

        public AiAnalysisRepository(AppDbContext db)
        {
            _db = db;
        }

        // SqlQuery 的查询投影；不是新建一张点位表的 EF 实体。
        public sealed class PointBinding
        {
            public string? Unit { get; set; }
        }

        // 返回同一一致性读中的配置和采样；不在此生成自然语言结论。
        // null 表示设备不存在、已停用或点位未启用，由 Service 转成 404。
        /*
        1. 确认“设备 + 点位”存在且启用
        2. 拿到这个点位的配置单位
        3. 取这个时间窗口内最新的 121 条样本
         */
        public async Task<AiCapture?> ReadWindowAsync(
            string device,
            string point,
            DateTime start,
            DateTime end,
            CancellationToken ct)
        {
            // 一致性读只覆盖取数，不把远程模型调用放进数据库事务。
            // 1.开启一个数据库事务； 2.隔离级别设为 RepeatableRead（可重复读）。
            await using var tx = await _db.Database.BeginTransactionAsync(  
                IsolationLevel.RepeatableRead, ct);     // RepeatableRead 保证这两次读来自同一个一致性快照。

            // 插值由 EF 作为数据库参数处理，不是字符串拼接 SQL。
            // 第一条 SQL：确认设备/点位，并拿到单位
            var bindings = await _db.Database.SqlQuery<PointBinding>($"""
                SELECT p.unit AS Unit FROM device_points p
                JOIN devices d ON d.id=p.device_id
                WHERE d.device_code={device} AND p.point_code={point}
                  AND d.is_deleted=0 AND d.status=1 AND p.enabled=1
                LIMIT 1
                """).ToListAsync(ct);

            if (bindings.Count == 0)
            {
                //查不到，说明设备/点位不存在、被删除、停用，或点位未启用；
                await tx.RollbackAsync(ct);// 回滚事务，返回 null；
                return null;        //  Service 收到 null 后抛 404
            }

            // 多取 1 条判断窗口是否超限；不把截断后的 120 条冒充完整窗口。
            //  第二条查询：取窗口内最新的 121 条样本
            var rows = await _db.Set<DeviceData>()
                .AsNoTracking()
                .Where(x => x.DeviceCode == device && x.PointCode == point
                    && x.OccurredAt >= start && x.OccurredAt < end)
                .OrderByDescending(x => x.OccurredAt)   // 按时间降序排列，最新的样本在前
                .ThenByDescending(x => x.Id)            // 如果时间相同，则按 ID 降序排列，保证顺序一致
                .Take(121)                              // 取121条样本  120 是业务上限，121 是“多取一条用来发现超限”
                .Select(x => new AiSample(              // 投影成 AiSample     数据库实体DeviceData -》 AiSample Dto
                    x.Id,
                    x.ValueDecimal,
                    x.Unit,
                    x.Quality,
                    x.OccurredAt))
                .ToListAsync(ct);   // 执行查询: 把 SQL 发给数据库，异步取回列表；ct 让查询可以被取消。

            await tx.CommitAsync(ct);   // 提交事务，释放数据库资源
            // bindings[0].Unit：前面已经保证 Count > 0，所以取第一个是安全的；  AiCapture：把“配置单位 + 样本列表”一起返回给 Service。
            return new AiCapture(bindings[0].Unit, rows);   // 返回配置单位和样本列表，供 Service 层做质量检查和模型输入构建
        }

        // AsNoTracking 用于只读回查，避免把已有请求当成待修改实体。
        public Task<AiAnalysisRecord?> FindRequestAsync(
            string requestId,
            CancellationToken ct) =>
            _db.Set<AiAnalysisRecord>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == requestId, ct);

        // 尝试持久化初始记录；唯一冲突返回 false，其余数据库错误继续抛出。
        public async Task<bool> TryAddAsync(AiAnalysisRecord row, CancellationToken ct)
        {
            _db.Set<AiAnalysisRecord>().Add(row);   // 把新记录标记为 Added 状态，准备插入数据库

            try
            {
                await _db.SaveChangesAsync(ct);     // 尝试将所有更改保存到数据库，包括插入新记录
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                // 并发重复 requestId 由数据库唯一索引裁决，移除失败的 Added 实体。
                _db.Entry(row).State = EntityState.Detached;
                return false;
            }
        }

        // 复用本次请求的 DbContext 工作单元；模型结束后只提交记录状态。
        public async Task SaveAsync(CancellationToken ct) => await _db.SaveChangesAsync(ct);
    }
}
