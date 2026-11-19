using System.Text;
using IotGatewayLearning.DeviceSimulator.Simulators;

namespace IotGatewayLearning.DeviceSimulator
{
    /// <summary>
    /// 模拟器总入口。它只做一件事：看第一个参数，决定启动哪个"假设备"。
    /// 真正的模拟逻辑都在 Simulators 文件夹里，一个设备一个文件。
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // 第一个参数：模式；不传默认 all（同时启动 COM12 串口温度仪表 + Modbus 虚拟 PLC）
            var mode = "all";
            if (args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]))
            {
                mode = args[0].Trim().ToLowerInvariant();
            }

            // 第二个参数：仪表端串口；不传默认 COM12
            var portName = "COM12";
            if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
            {
                portName = args[1].Trim();
            }

            switch (mode)
            {
                case "all":
                    await Task.WhenAll(
                        SerialTemperatureSimulator.RunAsync(portName),
                        ModbusTcpSimulator.RunAsync());
                    break;

                case "serial":
                    await SerialTemperatureSimulator.RunAsync(portName);
                    break;

                case "modbus":
                    // Modbus TCP 模式不需要串口参数
                    await ModbusTcpSimulator.RunAsync();
                    break;

                case "mqtt":
                    // Day14：连接 EMQX，模拟一台会发遥测、收命令的空调
                    await MqttAirConditionerSimulator.RunAsync();
                    break;

                default:
                    Console.WriteLine("未知模式。可用命令：all [端口名]、serial [端口名]、modbus 或 mqtt");
                    break;
            }
        }
    }
}
