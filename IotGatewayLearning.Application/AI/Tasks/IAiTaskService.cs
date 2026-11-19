using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Application.AI.Tasks
{
    public interface IAiTaskService
    {
        Task<AiToolTask> CreateAsync(StartTask request, string actor, CancellationToken ct);
        Task<List<AiToolTask>> ListAsync(string actor, CancellationToken ct);
        Task<AiToolTask> GetAsync(long id, string actor, CancellationToken ct);
    }
}
