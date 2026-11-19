using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    public sealed partial class AiTasksController
    {
        // GET 只回读，不把刷新按钮变成新的分析请求。
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AiToolTask>>>> List(CancellationToken ct)
        {
            var data = await _service.ListAsync(Actor, ct);

            return Ok(new ApiResponse<List<AiToolTask>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
