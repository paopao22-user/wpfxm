using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    public interface IAdvisoryRepository
    {
        // 回收超时 Sending，再条件抢占一条到期的 Pending/Retry。
        Task<AiNotification?> ClaimAsync(CancellationToken ct);

        Task<AiAdvisory?> GetAsync(string id, CancellationToken ct);

        // 只更新仍处于 Sending 的待办，写终态与下次重试时间。
        Task DeliveredAsync(AiNotification n, string state, string error, CancellationToken ct);

        // 同一 RunId 只保留一次研判。
        Task<AiAdvisory?> ByRunAsync(string runId, CancellationToken ct);

        // 汇总研判证据：巡检证据 + 回答 + 知识库引用 + 维修记录。
        Task<object> EvidenceAsync(InspectionRun run, long? answerId, CancellationToken ct);

        // 先落 Running 占住 RunId 唯一键；重复提交返回 false。
        Task<bool> StartAsync(AiAdvisory row, CancellationToken ct);

        // 研判结果与通知待办在同一个 SaveChanges 事务里提交。
        Task CompleteAsync(AiAdvisory row, bool notify, CancellationToken ct);

        Task<List<AiAdvisory>> ListAsync(string owner, CancellationToken ct);

        // 条件更新：确认只从 Active，关闭只从 Acknowledged。
        Task<bool> HandleAsync(
            string id,
            string owner,
            bool close,
            string remark,
            CancellationToken ct);

        Task<List<AiNotification>> NotificationsAsync(string owner, CancellationToken ct);
    }
}
