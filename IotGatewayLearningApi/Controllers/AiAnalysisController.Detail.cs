using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI;
using IotGatewayLearning.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    public sealed partial class AiAnalysisController
    {
        [HttpGet("{id:long}")]
        // 路由 id 只定位资源，Service 与 Repository 共同检查访问归属。
        public async Task<ActionResult<ApiResponse<AiAnalysisDetail>>> Get(
            long id,
            CancellationToken ct)
        {
            var data = await _service.GetAsync(id, User.Identity?.Name ?? "", ct);

            return Ok(new ApiResponse<AiAnalysisDetail>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
