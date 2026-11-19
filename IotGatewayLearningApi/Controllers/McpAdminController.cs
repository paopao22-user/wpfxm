using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Mcp;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearning.Domain.AI.Mcp;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    /// <summary>
    /// MCP 工具管理：工具目录、设备授权、试调用、授权变更、调用审计。
    /// 本项目没有暴露 /mcp 协议端点，工具目录与执行入口都在 McpAccessService 里。
    /// </summary>
    [ApiController]
    [Route("api/mcp-admin")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public sealed class McpAdminController : ControllerBase
    {
        private readonly McpAccessService _service;

        public McpAdminController(McpAccessService service) => _service = service;

        // 管理操作员只从已验证 JWT 取；请求体里的 actor 是被授权人，两者不同。
        private string OperatorName => User.Identity?.Name ?? string.Empty;

        [HttpGet("tools")]
        public ActionResult<ApiResponse<IReadOnlyList<ToolInfo>>> Tools()
        {
            return Ok(new ApiResponse<IReadOnlyList<ToolInfo>>
            {
                Code = 200,
                Message = "成功",
                Data = _service.Tools
            });
        }

        [HttpGet("grants")]
        public async Task<ActionResult<ApiResponse<List<McpGrant>>>> Grants(CancellationToken ct)
        {
            var data = await _service.GrantsAsync(ct);

            return Ok(new ApiResponse<List<McpGrant>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpPut("grants")]
        public async Task<ActionResult<ApiResponse<bool>>> Grant(
            [FromBody] McpGrantRequest request,
            CancellationToken ct)
        {
            await _service.ChangeAsync(request, OperatorName, grant: true, ct);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "成功",
                Data = true
            });
        }

        [HttpPost("grants/revoke")]
        public async Task<ActionResult<ApiResponse<bool>>> Revoke(
            [FromBody] McpGrantRequest request,
            CancellationToken ct)
        {
            await _service.ChangeAsync(request, OperatorName, grant: false, ct);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "成功",
                Data = true
            });
        }

        [HttpPost("invoke")]
        public async Task<ActionResult<ApiResponse<McpReadResult>>> Invoke(
            [FromBody] McpInvokeRequest request,
            CancellationToken ct)
        {
            var data = await _service.InvokeAsync(OperatorName, request, ct);

            return Ok(new ApiResponse<McpReadResult>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("changes")]
        public async Task<ActionResult<ApiResponse<List<McpGrantChange>>>> Changes(CancellationToken ct)
        {
            var data = await _service.ChangesAsync(ct);

            return Ok(new ApiResponse<List<McpGrantChange>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("calls")]
        public async Task<ActionResult<ApiResponse<List<McpCall>>>> Calls(CancellationToken ct)
        {
            var data = await _service.CallsAsync(ct);

            return Ok(new ApiResponse<List<McpCall>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("calls/{id}")]
        public async Task<ActionResult<ApiResponse<McpCall>>> Call(
            string id,
            CancellationToken ct)
        {
            var data = await _service.CallAsync(id, ct);

            return Ok(new ApiResponse<McpCall>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
