using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI;
using IotGatewayLearning.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    public sealed partial class AiAnalysisController
    {
        [HttpGet]
        // FromQuery 读取 URL 参数，创建者仍从 JWT 提取。
        public async Task<ActionResult<ApiResponse<AiAnalysisPage>>> List(
            [FromQuery] string deviceCode,
            [FromQuery] long? beforeId,
            CancellationToken ct)
        {
            var data = await _service.ListAsync(
                deviceCode, beforeId, User.Identity?.Name ?? "", ct);

            return Ok(new ApiResponse<AiAnalysisPage>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
