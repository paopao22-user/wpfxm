using IotGatewayLearning.Application.DTOs.Serial;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/serial-diagnostics")]
    public class SerialDiagnosticsController : ControllerBase
    {
        private readonly ISerialDiagnosticService _service;

        public SerialDiagnosticsController(ISerialDiagnosticService service)
        {
            _service = service;
        }

        /// <summary>
        /// 查询服务器可见、且在白名单中的串口
        /// </summary>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpGet("ports")]
        public ActionResult<ApiResponse<IReadOnlyList<string>>> GetPorts()
        {
            var ports = _service.GetAvailablePorts();

            return Ok(new ApiResponse<IReadOnlyList<string>>
            {
                Code = 200,
                Message = "串口列表查询成功",
                Data = ports
            });
        }

        /// <summary>
        /// 发送一条命令，等待一条完整响应
        /// </summary>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPost("send-and-receive")]
        public async Task<ActionResult<ApiResponse<SerialCommandResponse>>> SendAndReceive(
            [FromBody] SerialCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _service.SendAndReceiveAsync(request, cancellationToken);

            return Ok(new ApiResponse<SerialCommandResponse>
            {
                Code = 200,
                Message = "串口问答成功",
                Data = result
            });
        }
    }
}
