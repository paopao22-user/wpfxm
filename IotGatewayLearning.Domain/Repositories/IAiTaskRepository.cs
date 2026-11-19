using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    public interface IAiTaskRepository
    {
        Task<AiToolTask?> ByKeyAsync(string key, CancellationToken ct);
        Task<bool> AddAsync(AiToolTask task, CancellationToken ct);
        Task SaveAsync(CancellationToken ct);
        Task<List<AiToolTask>> ListAsync(string actor, CancellationToken ct);
        Task<AiToolTask?> GetAsync(long id, string actor, CancellationToken ct);
    }
}
