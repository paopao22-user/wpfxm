using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.DTOs.Control;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    /// <summary>
    /// 控制申请人工审批：提交、本人列表、审批队列、详情、批准/驳回、撤销。
    /// 审批通过也不会下发设备命令，执行在 Day51。
    /// </summary>
    [ApiController]
    [Route("api/control-requests")]
    public sealed class ControlRequestsController : ControllerBase
    {
        private readonly IControlRequestService _service;

        public ControlRequestsController(IControlRequestService service) => _service = service;

        private string Actor => User.Identity?.Name ?? string.Empty;

        // 审批权限只从 JWT 的 permission Claim 判断，不看客户端传参。
        private bool CanApprove => User.HasClaim("permission", PermissionCodes.DeviceApprove);

        [HttpPost]
        [Authorize(Policy = PermissionCodes.DeviceControl)]
        public async Task<ActionResult<ApiResponse<object>>> Submit(
            [FromBody] SubmitControl request,
            CancellationToken ct)
        {
            var row = await _service.SubmitAsync(request, Actor, ct);

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "操作成功",
                Data = new { id = row.Id, state = row.State }
            });
        }

        /// <summary>
        /// review=false 查本人申请；review=true 查待审批队列（需要 device:approve）。
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ControlView)]
        public async Task<ActionResult<ApiResponse<List<ControlRequest>>>> List(
            [FromQuery] bool review,
            CancellationToken ct)
        {
            var data = await _service.ListAsync(Actor, review, CanApprove, ct);

            return Ok(new ApiResponse<List<ControlRequest>>
            {
                Code = 200,
                Message = "操作成功",
                Data = data
            });
        }

        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.ControlView)]
        public async Task<ActionResult<ApiResponse<object>>> Detail(
            string id,
            CancellationToken ct)
        {
            var data = await _service.DetailAsync(id, Actor, CanApprove, ct);

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "操作成功",
                Data = data
            });
        }

        /// <summary>
        /// 批准/驳回共用同一个接口：approve=true 批准，false 驳回。
        /// </summary>
        [HttpPost("{id}/decision")]
        [Authorize(Policy = PermissionCodes.DeviceApprove)]
        public async Task<ActionResult<ApiResponse<object>>> Decide(
            string id,
            [FromBody] ReviewControl request,
            CancellationToken ct)
        {
            var row = await _service.DecideAsync(id, Actor, request.Approve, request.Remark, ct);

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "操作成功",
                Data = new { id = row.Id, state = row.State }
            });
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = PermissionCodes.DeviceControl)]
        public async Task<ActionResult<ApiResponse<object>>> Cancel(
            string id,
            [FromBody] CancelControl request,
            CancellationToken ct)
        {
            var row = await _service.CancelAsync(id, Actor, request.Remark, ct);

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "操作成功",
                Data = new { id = row.Id, state = row.State }
            });
        }
    }
}
