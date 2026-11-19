using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    public interface IMcpRepository
    {
        // 账号存在且启用（users.status = 1）。
        Task<bool> ActorExistsAsync(string actor, CancellationToken ct);

        // 设备存在、未删除且启用。
        Task<bool> DeviceExistsAsync(string device, CancellationToken ct);

        // 精确三元组授权检查。
        Task<bool> AllowedAsync(string actor, string device, string tool, CancellationToken ct);

        // grant=true 授予（重复授权只刷新时间与操作员），false 撤销；同时写变更审计。
        Task ChangeAsync(
            string actor,
            string device,
            string tool,
            string operatorName,
            bool grant,
            CancellationToken ct);

        Task<List<McpGrant>> GrantsAsync(CancellationToken ct);

        Task<List<McpGrantChange>> ChangesAsync(CancellationToken ct);

        Task AddCallAsync(McpCall call, CancellationToken ct);

        Task CompleteAsync(string id, string status, string result, CancellationToken ct);

        Task<List<McpCall>> CallsAsync(CancellationToken ct);

        Task<McpCall?> CallAsync(string id, CancellationToken ct);
    }
}
