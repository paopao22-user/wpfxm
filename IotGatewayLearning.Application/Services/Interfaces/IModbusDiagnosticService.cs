using IotGatewayLearning.Application.DTOs.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// Modbus 诊断能力。Application 只定义合同，不认识 NModbus 和 TcpClient。
    /// </summary>
    public interface IModbusDiagnosticService
    {
        Task<ModbusReadTestResponse> ReadRegistersAsync(
            ModbusReadTestRequest request,
            CancellationToken cancellationToken = default);

        Task<ModbusWriteRegisterTestResponse> WriteRegisterAsync(
            ModbusWriteRegisterTestRequest request,
            CancellationToken cancellationToken = default);
    }
}
