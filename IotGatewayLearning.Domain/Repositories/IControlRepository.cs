using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    public interface IControlRepository
    {
        Task<ControlRequest?> FindAsync(string id, CancellationToken ct);

        Task<ControlRequest?> ByKeyAsync(string key, CancellationToken ct);

        Task<List<ControlAudit>> AuditsAsync(string id, CancellationToken ct);

        Task<bool> DeviceExistsAsync(string code, CancellationToken ct);

        Task<List<ControlRequest>> ListAsync(string actor, bool review, CancellationToken ct);

        // 申请与 Submit 审计同一事务；1062 冲突回读原申请。
        Task<ControlRequest> AddAsync(ControlRequest row, CancellationToken ct);

        // 条件更新 + Approve/Reject 审计同一事务；影响行数不是 1 返回 false。
        Task<bool> DecideAsync(
            string id,
            string actor,
            bool approve,
            string remark,
            CancellationToken ct);

        // 条件更新 + Cancel 审计同一事务。
        Task<bool> CancelAsync(string id, string actor, string remark, CancellationToken ct);
    }
}
