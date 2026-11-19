using NModbus;
using NModbus.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Simulators
{
    /// <summary>
    /// 模拟一台 Unit Id 为 1 的 Modbus TCP PLC。
    /// 它没有梯形图、没有真实传感器，但有真实的：TCP 端口、站号、寄存器区、功能码 03/06。
    /// </summary>
    public static class ModbusTcpSimulator
    {
        public static async Task RunAsync()
        {
            // 实验端口用 1502，不用标准的 502：
            //   ① Windows 上绑定 < 1024 的端口有时需要管理员权限
            //   ② 开发电脑上 502 可能被别的软件占用
            //   真实 PLC 仍然用 502，1502 只是本课程的实验约定
            const int listenPort = 1502;

            // Unit Id / 站号固定为 1（真实 PLC 由拨码开关或厂商软件设定，
            // 必须和请求里的 unitId 一致，否则 PLC 不响应）
            const byte unitId = 1;

            // 虚拟 PLC 的内存数据区
            var dataStore = new SlaveDataStore();

            // 地址 0 放 825 → 按 scale 0.1 换算成 82.5 ℃
            // 地址 1 放 250 → 25.0 ℃（设定值，后面 13.3 的写接口会改它）
            dataStore.HoldingRegisters.WritePoints(
                0,
                new ushort[] { 825, 250 });

            // 线圈 0 表示运行状态，初始为"停"
            dataStore.CoilDiscretes.WritePoints(
                0,
                new[] { false });

            // 创建 NModbus 工厂和 Unit Id 为 1 的从站
            var factory = new ModbusFactory();
            var slave = factory.CreateSlave(unitId, dataStore);

            // 只监听本机回环地址（不对局域网开放，安全）
            var listener = new TcpListener(IPAddress.Loopback, listenPort);

            // 创建从站网络，把虚拟 PLC 加进去
            var slaveNetwork = factory.CreateSlaveNetwork(listener);
            slaveNetwork.AddSlave(slave);

            // Ctrl+C 时通知监听和打印循环停止
            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            Console.WriteLine("虚拟 PLC 已启动");
            Console.WriteLine($"地址：127.0.0.1:{listenPort}");
            Console.WriteLine($"Unit Id：{unitId}");
            Console.WriteLine("Holding[0] = 825 -> 82.5 ℃");
            Console.WriteLine("Holding[1] = 250 -> 25.0 ℃");
            Console.WriteLine("按 Ctrl+C 停止。");
            Console.WriteLine();

            // 开始在 1502 端口监听 Modbus TCP 请求
            var listenTask = slaveNetwork.ListenAsync(shutdown.Token);

            try
            {
                while (!shutdown.IsCancellationRequested)
                {
                    // 每 2 秒读一次自己的内存，观察写接口有没有改掉地址 1
                    var values = dataStore.HoldingRegisters.ReadPoints(0, 2);

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] 原始温度={values[0]}，原始设定值={values[1]}");

                    await Task.Delay(TimeSpan.FromSeconds(2), shutdown.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ctrl+C 属于正常停止
            }
            finally
            {
                try
                {
                    await listenTask;
                }
                catch (OperationCanceledException)
                {
                    // 监听任务用同一个取消令牌结束
                }

                Console.WriteLine("虚拟 PLC 已停止。");
            }
        }
    }
}
