using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    // 单个采样：只保留模型需要的字段，不上传设备备注、账号等无关资料。
    public sealed record AiSample(
        long Id,
        decimal? Value,
        string? Unit,
        string Quality,
        DateTime OccurredAt);

    // 仓储取数结果：点位配置单位 + 本次读取到的样本。
    public sealed record AiCapture(
        string? ConfiguredUnit,
        List<AiSample> Samples);

    // 仓储契约：实现在 Infrastructure/Repositories/Implementations/AiAnalysisRepository.cs。
    public interface IAiAnalysisRepository
    {
        // device/point 定位点位；start 包含、end 不包含；ct 传递请求取消。
        // 返回 null：设备不存在、已停用或点位未启用，由 Service 转成 404；
        // 非 null 但 Samples 为空：点位存在但窗口内没有数据，走 InsufficientData。
        Task<AiCapture?> ReadWindowAsync(
            string device,
            string point,
            DateTime start,
            DateTime end,
            CancellationToken ct);

        // 按 UUID 查重复请求；null 表示尚未创建记录。
        Task<AiAnalysisRecord?> FindRequestAsync(
            string requestId,
            CancellationToken ct);

        // true：本次成功占用 requestId；false：并发请求已创建同一记录。
        Task<bool> TryAddAsync(
            AiAnalysisRecord row,
            CancellationToken ct);

        // 提交当前 DbContext 跟踪的变更，不调用模型。
        Task SaveAsync(CancellationToken ct);

        // actor 限定创建者；beforeId 是上一页末尾 ID，不是页码。
        Task<List<AiAnalysisRecord>> ListAsync(
            string actor,
            string device,
            long? beforeId,
            CancellationToken ct);

        // 同时校验记录 ID 与归属；不存在或无权访问均返回 null。
        Task<AiAnalysisRecord?> FindAsync(
            long id,
            string actor,
            CancellationToken ct);
    }
}
