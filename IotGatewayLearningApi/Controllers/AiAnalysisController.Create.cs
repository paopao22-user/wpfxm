using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI;
using IotGatewayLearning.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    public sealed partial class AiAnalysisController
    {
        [HttpPost]
        // FromBody 将 JSON 绑定为请求对象，API 层只调用业务服务。
        public async Task<ActionResult<ApiResponse<AiAnalysisDetail>>> Create(
            [FromBody] CreateAiAnalysisRequest request,
            CancellationToken ct)
        {
            // actor 从已验证 JWT 取得，不接受客户端指定创建人。
            var data = await _service.CreateAsync(request, User.Identity?.Name ?? "", ct);

            return Ok(new ApiResponse<AiAnalysisDetail>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
