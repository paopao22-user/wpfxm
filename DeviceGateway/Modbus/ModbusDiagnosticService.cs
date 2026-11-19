using IotGatewayLearning.Application.DTOs.Modbus;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using NModbus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceGateway.Modbus
{
    public sealed class ModbusDiagnosticService : IModbusDiagnosticService
    {
        // 主机白名单（不区分大小写）
        private readonly HashSet<string> _allowedHosts;

        public ModbusDiagnosticService(ModbusDiagnosticsOptions options)
        {
            _allowedHosts = new HashSet<string>(
                options.AllowedHosts,
                StringComparer.OrdinalIgnoreCase);
        }

        // ============================================================
        // 读：连接 → 读寄存器 → 解码 → 换算
        // ============================================================
        public async Task<ModbusReadTestResponse> ReadRegistersAsync(ModbusReadTestRequest request,CancellationToken cancellationToken = default)
        {
            // 网络请求之前先校验参数，错误参数不该真的去连 PLC
            ValidateHost(request.Host);
            ValidateReadRequest(request);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // ① TcpClient 负责建立到 PLC 的 TCP 连接
                using var tcpClient = new TcpClient();

                // ② 单独给"连接阶段"一个超时（不然不存在的 IP 会卡很久）
                using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectTimeout.CancelAfter(request.ConnectTimeoutMs);

                // 异步连接到 PLC，超时会抛出 OperationCanceledException
                await tcpClient.ConnectAsync(request.Host,request.Port,connectTimeout.Token);

                // ③ 用已连接的 TcpClient 创建 Modbus 主站
                var factory = new ModbusFactory();
                using var master = factory.CreateMaster(tcpClient);     // master 负责发 Modbus 请求、收响应。
                                                                        // 它负责把pdu套上7字头的MBAP头，发到tcpClient里去。它不负责连接，也不负责解码寄存器。

                // ④ 传输层超时；调试阶段不自动重试
                master.Transport.ReadTimeout = request.ReadTimeoutMs;
                master.Transport.Retries = 0;

                // ⑤ holding 用功能码 03，input 用功能码 04
                Task<ushort[]> readTask;                                    // ① 准备一个装“提货单”的变量
                switch (request.RegisterArea.Trim().ToLowerInvariant())     // ② 根据寄存器区，选择下单方式
                {
                    case "holding":
                        readTask = master.ReadHoldingRegistersAsync(        //    保持寄存器 → 功能码 03
                            request.UnitId,
                            request.ProtocolAddress,
                            request.RegisterCount);
                        break;

                    case "input":
                        readTask = master.ReadInputRegistersAsync(          //    输入寄存器 → 功能码 04
                            request.UnitId,
                            request.ProtocolAddress,
                            request.RegisterCount);
                        break;

                    default:                                                //    其他 → 直接报错
                        throw new AppException("数值读取只支持 holding 或 input 寄存器区", 400);
                }

                // ⑥ 再加一层 Task 级超时，保证超过限制一定返回错误
                var registers = await readTask.WaitAsync(                   // ③ 统一等待，统一超时
                    TimeSpan.FromMilliseconds(request.ReadTimeoutMs),
                    cancellationToken);

                // ⑦ ushort[] → 数值（这一步是解码器干的）
                var rawValue = ModbusRegisterDecoder.Decode(
                    registers,
                    request.DataType,
                    request.ByteOrder);

                // ⑧ 原始值 → 工程值（825 × 0.1 + 0 = 82.5）
                var engineeringValue = rawValue * request.Scale + request.Offset;

                stopwatch.Stop();           //停表

                return new ModbusReadTestResponse       // 组装响应对象，返回给上层
                {
                    Registers = registers,
                    RawValue = rawValue,
                    EngineeringValue = engineeringValue,
                    Quality = "Good",
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    OccurredAt = DateTime.UtcNow
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // 外部没取消 → 是我们自己设的连接超时
                throw new AppException("连接 PLC 超时，请检查 IP、端口和网络", 504);
            }
            catch (TimeoutException)
            {
                throw new AppException("Modbus 读取超时，请检查站号、地址和设备状态", 504);
            }
            catch (SocketException exception)
            {
                throw new AppException($"无法连接 PLC：{exception.SocketErrorCode}", 502);
            }
            catch (IOException exception)
            {
                throw new AppException($"Modbus 通信失败：{exception.Message}", 502);
            }
            catch (SlaveException exception)
            {
                // PLC 明确返回了"非法地址/非法功能"这类异常响应
                throw new AppException($"PLC 返回 Modbus 异常：{exception.Message}", 502);
            }
        }

        // ============================================================
        // 写：工程值 → 反推原始值 → 功能码 06
        // ============================================================
        public async Task<ModbusWriteRegisterTestResponse> WriteRegisterAsync(ModbusWriteRegisterTestRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateHost(request.Host);

            // 必须明确确认，防止界面上误点一下就把 PLC 改了
            if (!request.Confirmed)
            {
                throw new AppException("写寄存器前必须明确确认操作", 400);
            }

            if (request.Scale == 0)
            {
                throw new AppException("scale 不能为 0", 400);
            }

            // 反向换算：工程值 = 原始值 × scale + offset
            //          → 原始值 = (工程值 - offset) / scale
            var calculatedRaw = (request.EngineeringValue - request.Offset) / request.Scale;

            // 单寄存器只能放整数，四舍五入
            var roundedRaw = Math.Round(calculatedRaw, MidpointRounding.AwayFromZero);

            ushort rawValue;
            switch (request.DataType.Trim().ToUpperInvariant())
            {
                case "UINT16":
                    if (roundedRaw < ushort.MinValue || roundedRaw > ushort.MaxValue)
                    {
                        throw new AppException("换算后的原始值超出 UInt16 范围", 400);
                    }
                    rawValue = (ushort)roundedRaw;
                    break;

                case "INT16":
                    if (roundedRaw < short.MinValue || roundedRaw > short.MaxValue)
                    {
                        throw new AppException("换算后的原始值超出 Int16 范围", 400);
                    }
                    rawValue = unchecked((ushort)(short)roundedRaw);
                    break;

                default:
                    throw new AppException("单寄存器写入只支持 UInt16 或 Int16", 400);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var tcpClient = new TcpClient();
                using var connectTimeout = CancellationTokenSource
                    .CreateLinkedTokenSource(cancellationToken);
                connectTimeout.CancelAfter(request.ConnectTimeoutMs);

                await tcpClient.ConnectAsync(           
                    request.Host,
                    request.Port,
                    connectTimeout.Token);

                var factory = new ModbusFactory();
                using var master = factory.CreateMaster(tcpClient);
                master.Transport.ReadTimeout = request.ReadTimeoutMs;
                master.Transport.Retries = 0;

                // 功能码 06：向指定站号的零基地址写一个保持寄存器
                await master.WriteSingleRegisterAsync(
                        request.UnitId,
                        request.ProtocolAddress,
                        rawValue)
                    .WaitAsync(
                        TimeSpan.FromMilliseconds(request.ReadTimeoutMs),
                        cancellationToken);

                stopwatch.Stop();

                return new ModbusWriteRegisterTestResponse
                {
                    WrittenRawValue = rawValue,
                    EngineeringValue = request.EngineeringValue,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    OccurredAt = DateTime.UtcNow
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new AppException("连接 PLC 超时", 504);
            }
            catch (TimeoutException)
            {
                // 注意措辞：超时 ≠ 没执行
                throw new AppException("Modbus 写入超时，不能假定设备已经执行", 504);
            }
            catch (SocketException exception)
            {
                throw new AppException($"无法连接 PLC：{exception.SocketErrorCode}", 502);
            }
            catch (IOException exception)
            {
                throw new AppException($"Modbus 写入通信失败：{exception.Message}", 502);
            }
            catch (SlaveException exception)
            {
                throw new AppException($"PLC 拒绝写入：{exception.Message}", 502);
            }
        }

        // ============================================================
        // 校验帮手
        // ============================================================
        private void ValidateHost(string host)
        {
            if (!_allowedHosts.Contains(host.Trim()))
            {
                throw new AppException($"主机 {host} 不在 Modbus 诊断白名单中", 403);
            }
        }

        private static void ValidateReadRequest(ModbusReadTestRequest request)
        {
            var required = ModbusRegisterDecoder.GetRequiredRegisterCount(request.DataType);    // 这个数据类型需要几个寄存器

            if (request.RegisterCount != required)
            {
                throw new AppException($"{request.DataType} 必须读取 {required} 个寄存器，当前 registerCount 为 {request.RegisterCount}", 400);
            }

            if (!double.IsFinite(request.Scale) || request.Scale == 0)
            {
                throw new AppException("scale 必须是有效且非 0 的数字", 400);
            }
        }
    }
}
