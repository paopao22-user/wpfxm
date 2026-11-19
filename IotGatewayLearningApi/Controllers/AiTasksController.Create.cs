using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Tasks;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    public sealed partial class AiTasksController
    {
        // POST 按唯一请求编号创建；模型调用失败也返回持久化任务状态。
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AiToolTask>>> Create(
            [FromBody] StartTask request,
            CancellationToken ct)
        {
            var data = await _service.CreateAsync(request, Actor, ct);

            return Ok(new ApiResponse<AiToolTask>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
