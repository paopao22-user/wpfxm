using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Advisories;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    /// <summary>
    /// AI 告警研判：创建研判、列表、确认/关闭、通知投递状态。
    /// 这是对 Day31 规则告警的补充，不替代联锁，也不控制设备。
    /// </summary>
    [ApiController]
    [Route("api/ai/advisories")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public sealed class AdvisoriesController : ControllerBase
    {
        private readonly AdvisoryService _service;

        public AdvisoriesController(AdvisoryService service) => _service = service;

        private string Actor => User.Identity?.Name ?? string.Empty;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<AiAdvisory>>> Create(
            [FromBody] CreateAdvisory request,
            CancellationToken ct)
        {
            var data = await _service.CreateAsync(request, Actor, ct);

            return Ok(new ApiResponse<AiAdvisory>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AiAdvisory>>>> List(CancellationToken ct)
        {
            var data = await _service.ListAsync(Actor, ct);

            return Ok(new ApiResponse<List<AiAdvisory>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        /// <summary>
        /// 确认/关闭共用同一个接口：
        /// close=false → Active 变 Acknowledged；close=true → Acknowledged 变 Closed。
        /// </summary>
        [HttpPost("{id}/handle")]
        public async Task<ActionResult<ApiResponse<bool>>> Handle(
            string id,
            [FromBody] HandleAdvisory request,
            CancellationToken ct)
        {
            var data = await _service.HandleAsync(id, Actor, request.Close, request.Remark, ct);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("notifications")]
        public async Task<ActionResult<ApiResponse<List<AiNotification>>>> Notifications(
            CancellationToken ct)
        {
            var data = await _service.NotificationsAsync(Actor, ct);

            return Ok(new ApiResponse<List<AiNotification>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
