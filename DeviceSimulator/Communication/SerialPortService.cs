using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Communication
{
    /// <summary>
    /// 【通信层】只负责打开端口、发裸字节、后台收裸字节。
    /// 它不认识 "PING"，也不认识温度，只认 byte[]。
    /// </summary>
    public sealed class SerialPortService : IDisposable
    {
        private readonly SerialPort _serialPort;        // 串口对象，封装了底层硬件的读写操作
        private CancellationTokenSource? _readCts;      // 用于取消后台异步读取循环任务的令牌源

        /// <summary>
        /// 当底层硬件读到任意裸字节时触发（交给分帧层处理）
        /// </summary>
        public event Action<byte[]>? RawDataReceived;   // 事件：当底层硬件读到任意裸字节时触发（交给分帧层处理）

        public bool IsOpen => _serialPort.IsOpen;       // 串口是否已打开
        public string PortName => _serialPort.PortName; // 串口名称

        public SerialPortService(string portName = "COM12", int baudRate = 9600)
        {
            _serialPort = new SerialPort(
                portName: portName,
                baudRate: baudRate,
                parity: Parity.None,
                dataBits: 8,
                stopBits: StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };
        }

        /// <summary>
        /// 打开串口，并启动后台异步读取循环任务
        /// </summary>
        public void Open()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                _readCts = new CancellationTokenSource();
                // 启动后台独立的非阻塞读取任务
                _ = Task.Run(() => ReadLoopAsync(_readCts.Token));
            }
        }


        /// <summary>
        /// 关闭串口并释放硬件句柄
        /// </summary>
        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _readCts?.Cancel();     // 取消后台读取循环任务
                _serialPort.Close();    // 关闭串口
            }
        }

        /// <summary>
        /// 发送裸字节流
        /// </summary>
        public void Send(byte[] data)
        {
            if (!_serialPort.IsOpen)
            {
                throw new InvalidOperationException($"串口 {_serialPort.PortName} 尚未打开，无法发送！");
            }

            _serialPort.Write(data, 0, data.Length);        // 同步阻塞发送
        }

        /// <summary>
        /// 底层异步读取循环：不断从硬件水管捞取水滴并抛出事件
        /// </summary>
        private async Task ReadLoopAsync(CancellationToken ct)
        {
            byte[] tempBuffer = new byte[1024];     // 临时缓冲区，用于存放每次从串口读到的碎片数据

            while (!ct.IsCancellationRequested && _serialPort.IsOpen)
            {
                try
                {
                    // 异步非阻塞等待：无数据时挂起让出 CPU，有数据时被操作系统唤醒
                    int bytesRead = await _serialPort.BaseStream.ReadAsync(tempBuffer, 0, tempBuffer.Length, ct);

                    // 处理读取到的数据
                    if (bytesRead > 0)
                    {
                        // 复制本次读出的真实数据
                        byte[] chunk = new byte[bytesRead];
                        Array.Copy(tempBuffer, 0, chunk, 0, bytesRead);

                        // 抛出事件，交付上层分帧器
                        RawDataReceived?.Invoke(chunk);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // 端口被拔出或底层错误
                    break;
                }
            }
        }

        /// <summary>
        /// 释放并清理底层串口及相关托管资源。
        /// </summary>
        /// <remarks>关闭串口，释放底层 SerialPort 实例，并释放任何关联的 CancellationTokenSource。调用后不应再使用该实例。</remarks>
        public void Dispose()
        {
            Close();                    // 关闭串口并取消读取任务
            _serialPort.Dispose();      // 释放底层串口资源
            _readCts?.Dispose();        // 释放 CancellationTokenSource
        }
    }
}
