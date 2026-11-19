using IotGatewayLearning.Application.DTOs.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 串口诊断能力。
    /// Application 只声明"要什么能力"，
    /// 具体怎么打开串口（System.IO.Ports）由 DeviceGateway 实现。
    /// </summary>
    public interface ISerialDiagnosticService
    {
        // 返回服务器可见、且在白名单里的串口
        IReadOnlyList<string> GetAvailablePorts();


        // 发一条命令，等一条完整响应
        Task<SerialCommandResponse> SendAndReceiveAsync(
            SerialCommandRequest request,
            CancellationToken cancellationToken = default);
    }
}
