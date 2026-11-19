using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.DeviceSimulator.Communication;
using IotGatewayLearning.DeviceSimulator.Framing;
using IotGatewayLearning.DeviceSimulator.Protocol;

namespace IotGatewayLearning.DeviceSimulator.Simulators
{
    /// <summary>
    /// 模拟一台连接在 COM12 上的串口温度仪表（Day12）。
    /// 三层组装：通信层（SerialPortService）→ 分帧层（DelimiterFrameDecoder）→ 协议层（TemperatureProtocol）。
    /// </summary>
    public static class SerialTemperatureSimulator
    {
        public static async Task RunAsync(string portName)
        {
            // ===== 三层组装 =====
            using var service = new SerialPortService(portName, 9600);  // 通信层
            var decoder = new DelimiterFrameDecoder();                   // 分帧层

            // 通信层收到裸字节 → 分帧层切帧 → 协议层解析 → 协议层造响应 → 通信层发回
            service.RawDataReceived += (chunk) =>
            {
                var frames = decoder.Feed(chunk);

                foreach (var frame in frames)
                {
                    try
                    {
                        string command = TemperatureProtocol.ParseCommand(frame);

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到命令：{command}");
                        Console.WriteLine($"                原始字节：{TemperatureProtocol.ToHexString(frame)}");

                        byte[] response;
                        switch (command)
                        {
                            case TemperatureProtocol.CommandPing:
                                response = TemperatureProtocol.BuildPongResponse();
                                break;

                            case TemperatureProtocol.CommandReadTemperature:
                                response = TemperatureProtocol.BuildTemperatureResponse(25.6);
                                break;

                            default:
                                response = TemperatureProtocol.BuildUnknownCommandResponse();
                                break;
                        }

                        service.Send(response);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 返回：{TemperatureProtocol.ToDisplayText(response)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[协议层] 解析失败：{ex.Message}");
                    }
                }
            };

            // Ctrl+C 优雅退出
            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            try
            {
                service.Open();

                Console.WriteLine("虚拟串口温度仪表已启动（通信层 → 分帧层 → 协议层）");
                Console.WriteLine($"端口：{portName}");
                Console.WriteLine("参数：9600,8,N,1；定界符：CRLF (0x0D 0x0A)");
                Console.WriteLine("支持命令：PING、READ:TEMP");
                Console.WriteLine("按 Ctrl+C 停止。");
                Console.WriteLine();

                // 一直挂着，直到 Ctrl+C
                await Task.Delay(Timeout.Infinite, shutdown.Token);
            }
            catch (OperationCanceledException)
            {
                // 正常退出
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"无法打开 {portName}：端口被其他程序占用，或权限不足。");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"串口通信中断：{ex.Message}");
            }
            finally
            {
                Console.WriteLine("虚拟串口温度仪表已停止。");
            }
        }
    }
}
