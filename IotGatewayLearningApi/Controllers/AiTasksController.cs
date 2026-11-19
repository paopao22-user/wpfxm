using IotGatewayLearning.Application.AI.Tasks;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/ai/tasks")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public sealed partial class AiTasksController : ControllerBase
    {
        private readonly IAiTaskService _service;

        public AiTasksController(IAiTaskService service)
        {
            _service = service;
        }

        // actor 只从已验证 JWT 取，不接受客户端指定。
        private string Actor => User.Identity?.Name ?? string.Empty;
    }
}
