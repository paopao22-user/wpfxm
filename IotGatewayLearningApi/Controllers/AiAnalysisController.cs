using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/ai/analyses")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public sealed partial class AiAnalysisController : ControllerBase
    {
        private readonly IAiAnalysisService _service;

        public AiAnalysisController(IAiAnalysisService service)
        {
            _service = service;
        }

        // 固定测试，无用户可控 Endpoint，避免把接口做成任意地址请求代理。
        [HttpPost("connection-test")]
        public async Task<ActionResult<ApiResponse<AiConnectionResult>>> TestConnection(
            CancellationToken ct)
        {
            var data = await _service.TestConnectionAsync(ct);

            return Ok(new ApiResponse<AiConnectionResult>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
