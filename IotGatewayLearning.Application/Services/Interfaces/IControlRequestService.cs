using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.DTOs.Control;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    public interface IControlRequestService
    {
        Task<ControlRequest> SubmitAsync(SubmitControl request, string actor, CancellationToken ct);

        // review=true 查审批队列，需要 canApprove；false 只查本人申请。
        Task<List<ControlRequest>> ListAsync(
            string actor,
            bool review,
            bool canApprove,
            CancellationToken ct);

        Task<object> DetailAsync(
            string id,
            string actor,
            bool canApprove,
            CancellationToken ct);

        Task<ControlRequest> DecideAsync(
            string id,
            string actor,
            bool approve,
            string remark,
            CancellationToken ct);

        Task<ControlRequest> CancelAsync(
            string id,
            string actor,
            string remark,
            CancellationToken ct);
    }
}
