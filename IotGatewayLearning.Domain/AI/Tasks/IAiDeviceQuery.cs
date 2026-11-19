using System;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.AI.Tasks
{
    public interface IAiDeviceQuery
    {
        Task<object> DeviceAsync(string device, CancellationToken ct);
        Task<object> HistoryAsync(string device, string point, DateTime end, CancellationToken ct);
        Task<object> AlarmsAsync(string device, CancellationToken ct);
    }
}
