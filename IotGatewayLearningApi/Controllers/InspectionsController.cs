using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Inspections;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    /// <summary>
    /// 设备巡检：计划管理、立即执行、启停、任务记录、证据查看、设备事件接入。
    /// HTTP 提交只返回 Queued；真正的分析在后台 Worker 里执行。
    /// </summary>
    [ApiController]
    [Route("api/ai/inspections")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public sealed class InspectionsController : ControllerBase
    {
        private readonly InspectionService _service;

        public InspectionsController(InspectionService service) => _service = service;

        private string Actor => User.Identity?.Name ?? string.Empty;

        [HttpPost("plans")]
        public async Task<ActionResult<ApiResponse<InspectionPlan>>> Create(
            [FromBody] SaveInspection request,
            CancellationToken ct)
        {
            var data = await _service.CreateAsync(request, Actor, ct);

            return Ok(new ApiResponse<InspectionPlan>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("plans")]
        public async Task<ActionResult<ApiResponse<List<InspectionPlan>>>> Plans(CancellationToken ct)
        {
            var data = await _service.PlansAsync(Actor, ct);

            return Ok(new ApiResponse<List<InspectionPlan>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpPost("plans/{id:long}/run")]
        public async Task<ActionResult<ApiResponse<InspectionRun>>> Run(
            long id,
            CancellationToken ct)
        {
            var data = await _service.SubmitAsync(id, Actor, "Manual", DateTimeOffset.UtcNow, ct);

            return Ok(new ApiResponse<InspectionRun>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpPut("plans/{id:long}/enabled")]
        public async Task<ActionResult<ApiResponse<bool>>> Enable(
            long id,
            [FromBody] bool value,
            CancellationToken ct)
        {
            var data = await _service.EnableAsync(id, Actor, value, ct);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("runs")]
        public async Task<ActionResult<ApiResponse<List<InspectionRun>>>> Runs(CancellationToken ct)
        {
            var data = await _service.RunsAsync(Actor, ct);

            return Ok(new ApiResponse<List<InspectionRun>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("runs/{id}")]
        public async Task<ActionResult<ApiResponse<InspectionRun>>> RunDetail(
            string id,
            CancellationToken ct)
        {
            // 不存在或不属于当前用户，统一返回 404，不泄露别人的任务是否存在。
            var data = await _service.RunAsync(id, Actor, ct)
                ?? throw new AppException("巡检任务不存在", 404);

            return Ok(new ApiResponse<InspectionRun>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        /// <summary>
        /// 设备事件入口：给受认证的桥接程序用。
        /// 额外要求 ai:event:ingest 权限，与计划管理权限分离，
        /// 避免任何能管计划的账号都能伪造设备事件。
        /// </summary>
        [HttpPost("events")]
        [Authorize(Policy = PermissionCodes.AiEventIngest)]
        public async Task<ActionResult<ApiResponse<InspectionRun>>> Event(
            [FromBody] InspectionEvent request,
            CancellationToken ct)
        {
            var data = await _service.EventAsync(request, ct);

            return Ok(new ApiResponse<InspectionRun>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
