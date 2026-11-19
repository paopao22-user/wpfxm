using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.AI.Tasks
{
    public interface IAiTaskRunner
    {
        Task<string> RunAsync(string question, DeviceQueryTools tools, CancellationToken ct);
    }
}
