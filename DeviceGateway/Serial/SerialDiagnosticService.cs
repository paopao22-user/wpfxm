using IotGatewayLearning.Application.DTOs.Serial;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceGateway.Serial
{
    public sealed class SerialDiagnosticService : ISerialDiagnosticService
    {

        // 端口白名单（不区分大小写）
        private readonly HashSet<string> _allowedPorts;

        //构造函数
        public SerialDiagnosticService(SerialDiagnosticsOptions options)            // ① 注入配置
        {
            _allowedPorts = new HashSet<string>(options.AllowedPorts, StringComparer.OrdinalIgnoreCase);        
        }


        // 返回操作系统可见、且在白名单里的串口
        public IReadOnlyList<string> GetAvailablePorts()                // ② 查串口
        {
            var allPorts = SerialPort.GetPortNames();       // 获取操作系统可见的所有串口名
            var result = new List<string>();                // 用于存储过滤后的可用串口名

            // 过滤：只保留白名单里的端口
            foreach (var port in allPorts)
            {
                if (_allowedPorts.Contains(port))
                {
                    result.Add(port);       // 添加到结果列表中
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);  // 按字母顺序排序，忽略大小写
            return result;
        }


        // SerialPort 主要是同步 API；放到线程池，避免占住 WebAPI 的请求线程
        public Task<SerialCommandResponse> SendAndReceiveAsync(SerialCommandRequest request, CancellationToken cancellationToken = default) // ③ 收发
        {
            return Task.Run(() => Execute(request, cancellationToken), cancellationToken);
        }


        // 真正干活：打开 → 清缓冲 → 发送 → 读字节 → 分帧 → 关闭
        /*
        01. 接收前端发过来的请求：串口名、比特率、发送文本、停止符、过期时间
        01.5：把串口连接里面需要的参数从request里面提取出来
        02. 打开串口连接
        03. 清空buffer里面的缓存
        04. 发送内容
        05. 读取返回的字节
        06. 分帧
        07. 关闭串口
         */
        private SerialCommandResponse Execute(SerialCommandRequest request, CancellationToken cancellationToken)        // ④ 真正的活
        {
            var portName = request.PortName.Trim();     //提取出来串口名
            if (!_allowedPorts.Contains(portName))      //看下白名单里面是否包含串口名，防止乱输入
            {
                throw new AppException($"串口 {portName} 不在诊断白名单中", 403);
            }

            // 正文 + 结束符 = 真正要发送的字节, 注意串口发送都是字节类型，需要将其他类型转为字节类型
            var payloadBytes = Encoding.ASCII.GetBytes(request.Payload.Trim());
            var terminatorBytes = ParseTerminator(request.Terminator);
            var bytesToSend = new byte[payloadBytes.Length + terminatorBytes.Length];   // 拼装起来的要发送文本

            Array.Copy(payloadBytes, 0, bytesToSend, 0, payloadBytes.Length);           // 复制前面的内容
            Array.Copy(terminatorBytes, 0, bytesToSend, payloadBytes.Length, terminatorBytes.Length);   // 复制后面的内容

            // 第一步：构造函数给"身份参数"
            //   端口名、波特率、校验位、数据位、停止位
            //   这五个决定"这条链路怎么通信"
            using var serialPort = new SerialPort(portName, request.BaudRate, Parity.None, 8, StopBits.One)
            {
                // 第二步：对象初始化器给"行为参数"
                //   流控、编码、超时
                //   这五个决定"通信时的策略"
                Handshake = Handshake.None,
                Encoding = Encoding.ASCII,
                ReadTimeout = 100,
                WriteTimeout = 2000
            };

            var stopwatch = Stopwatch.StartNew();
            var decoder = new DelimiterFrameDecoder();


            try
            {
                serialPort.Open();          // 打开串口

                // 清空缓冲区里面的缓存
                serialPort.DiscardInBuffer();       
                serialPort.DiscardOutBuffer();

                serialPort.Write(bytesToSend, 0, bytesToSend.Length);       // 发送内容

                // 在总超时内逐字节读，边读边喂分帧器
                while (stopwatch.ElapsedMilliseconds < request.ReadTimeoutMs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int value;
                    try
                    {
                        value = serialPort.ReadByte();      //一次读取一个byte
                    }
                    catch (TimeoutException)
                    {
                        // 100ms 内没字节到，属于正常空闲，继续等（总超时才判失败）
                        continue;
                    }

                    if(value < 0)
                    {
                        continue;
                    }

                    // 每收到 1 个字节就喂给分帧器；它自己负责攒够一帧
                    var frames = decoder.Feed(new byte[] { (byte)value });

                    if(frames.Count > 0)
                    {
                        stopwatch.Stop();       // 停止计时
                        return BuildResponse(bytesToSend, frames[0], stopwatch.ElapsedMilliseconds);
                    }
                }

                // 走完总超时还没拿到完整一帧
                throw new AppException($"等待串口响应超时（{request.ReadTimeoutMs} ms）", 504);
            }

            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();     //关闭串口
                }
            }
        }

        // 把"发出去的字节 + 切出来的帧 + 耗时"组装成响应
        private static SerialCommandResponse BuildResponse(byte[] sentBytes,byte[] frame,long elapsedMilliseconds)
        {
            return new SerialCommandResponse
            {
                SentHex = ToHex(sentBytes),

                // 帧里带着 CRLF，返回给前端时去掉首尾空白
                ReceivedText = Encoding.ASCII.GetString(frame).Trim(),

                ReceivedHex = ToHex(frame),
                ElapsedMilliseconds = elapsedMilliseconds,
                OccurredAt = DateTime.UtcNow
            };
        }

        // None / CR / LF / CRLF → 真实字节
        private static byte[] ParseTerminator(string terminator)
        {
            
            switch (terminator.Trim().ToUpperInvariant())
            {
                case "NONE":
                    return Array.Empty<byte>();
                case "CR":
                    return new byte[] { 0x0D };
                case "LF":
                    return new byte[] { 0x0A };
                case "CRLF":
                    return new byte[] { 0x0D, 0x0A };
                default:
                    throw new AppException("terminator 只支持 None / CR / LF / CRLF", 400);
            }
        }

        private static string ToHex(byte[] data)
        {
            return string.Join(" ", Array.ConvertAll(data, b => b.ToString("X2")));
        }
    }
}
