using IotGatewayLearning.Application.DTOs.Modbus;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/modbus-diagnostics")]
    public class ModbusDiagnosticsController : ControllerBase
    {
        private readonly IModbusDiagnosticService _service;

        public ModbusDiagnosticsController(IModbusDiagnosticService service)
        {
            _service = service;
        }

        /// <summary>
        /// 读取寄存器（只读诊断，需要"设备配置"权限）
        /// </summary>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPost("read-registers")]
        public async Task<ActionResult<ApiResponse<ModbusReadTestResponse>>> ReadRegisters(
            [FromBody] ModbusReadTestRequest request,CancellationToken cancellationToken)
        {
            var data = await _service.ReadRegistersAsync(request, cancellationToken);

            return Ok(new ApiResponse<ModbusReadTestResponse>
            {
                Code = 200,
                Message = "Modbus 读取成功",
                Data = data
            });
        }

        /// <summary>
        /// 写寄存器（会真的改设备，需要"设备控制"权限）
        /// </summary>
        [Authorize(Policy = PermissionCodes.DeviceControl)]
        [HttpPost("write-register")]
        public async Task<ActionResult<ApiResponse<ModbusWriteRegisterTestResponse>>> WriteRegister(
            [FromBody] ModbusWriteRegisterTestRequest request,CancellationToken cancellationToken)
        {
            var data = await _service.WriteRegisterAsync(request, cancellationToken);

            return Ok(new ApiResponse<ModbusWriteRegisterTestResponse>
            {
                Code = 200,
                Message = "Modbus 寄存器写入成功",
                Data = data
            });
        }
    }
}
