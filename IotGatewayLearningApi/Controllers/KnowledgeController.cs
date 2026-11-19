using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Knowledge;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearning.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    // 设备知识库：导入、列表、检索回答、历史回答、退役。
    [ApiController]
    [Route("api/ai/knowledge")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public sealed class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeService _service;

        public KnowledgeController(IKnowledgeService service) => _service = service;

        // 身份只从已验证 JWT 取，不接受客户端指定。
        private string Actor => User.Identity?.Name ?? string.Empty;

        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<KnowledgeDocument>>> Import(
            [FromBody] ImportKnowledge request,
            CancellationToken ct)
        {
            var data = await _service.ImportAsync(request, Actor, ct);

            return Ok(new ApiResponse<KnowledgeDocument>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<KnowledgeDocument>>>> List(CancellationToken ct)
        {
            var data = await _service.ListAsync(Actor, ct);

            return Ok(new ApiResponse<List<KnowledgeDocument>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ApiResponse<KnowledgeAnswer>>> Ask(
            [FromBody] AskKnowledge request,
            CancellationToken ct)
        {
            var data = await _service.AskAsync(request, Actor, ct);

            return Ok(new ApiResponse<KnowledgeAnswer>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpGet("answers")]
        public async Task<ActionResult<ApiResponse<List<KnowledgeAnswer>>>> Answers(CancellationToken ct)
        {
            var data = await _service.AnswersAsync(Actor, ct);

            return Ok(new ApiResponse<List<KnowledgeAnswer>>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }

        [HttpPost("{id:long}/retire")]
        public async Task<ActionResult<ApiResponse<KnowledgeDocument>>> Retire(
            long id,
            CancellationToken ct)
        {
            var data = await _service.RetireAsync(id, Actor, ct);

            return Ok(new ApiResponse<KnowledgeDocument>
            {
                Code = 200,
                Message = "成功",
                Data = data
            });
        }
    }
}
