using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    public sealed partial class AiTasksController
    {
        // 返回指定任务的真实执行记录，Service 继续核对记录归属。
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<AiToolTask>>> Get(long id, CancellationToken ct)
        {
            var data = await _service.GetAsync(id, Actor, ct);

            return Ok(new ApiResponse<AiToolTask>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
