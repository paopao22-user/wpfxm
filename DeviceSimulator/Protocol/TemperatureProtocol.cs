using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Protocol
{
    /// <summary>
    /// 【协议层】只负责"字节 ↔ 业务含义"的翻译。
    /// 前提：进来的 byte[] 已经是一整帧（由分帧层保证）。
    /// </summary>
    public static class TemperatureProtocol
    {
        private static readonly Encoding ProtocolEncoding = Encoding.ASCII; // 协议层使用 ASCII 编码

        // 命令常量：避免在各处手写字符串（写错一个字母编译不报错、运行才失效）
        public const string CommandPing = "PING";
        public const string CommandReadTemperature = "READ:TEMP";

        /// <summary>
        /// 把一帧命令解码成规范化文本（去掉 CRLF、转大写）。
        /// </summary>
        public static string ParseCommand(byte[] frame)
        {
            if (frame == null || frame.Length == 0)
            {
                throw new ArgumentException("命令帧为空，无法解析。");
            }

            string text = ProtocolEncoding.GetString(frame).Trim();

            if (text.Length == 0)
            {
                throw new FormatException("命令帧去掉空白后为空。");
            }

            return text.ToUpperInvariant();
        }

        /// <summary>构造 PONG 响应。</summary>
        public static byte[] BuildPongResponse()
        {
            return ProtocolEncoding.GetBytes("PONG\r\n");
        }

        /// <summary>构造温度响应，例如 "TEMP=25.6\r\n"。</summary>
        public static byte[] BuildTemperatureResponse(double temperature)
        {
            return ProtocolEncoding.GetBytes($"TEMP={temperature:F1}\r\n");
        }

        /// <summary>构造"不支持的命令"响应。</summary>
        public static byte[] BuildUnknownCommandResponse()
        {
            return ProtocolEncoding.GetBytes("ERR:UNKNOWN\r\n");
        }

        /// <summary>HEX 显示，排错用。</summary>
        public static string ToHexString(byte[] data)
        {
            return string.Join(" ", Array.ConvertAll(data, b => b.ToString("X2")));
        }

        /// <summary>把响应字节转成能看的文本（CRLF 显示成 \r\n 两个符号）。</summary>
        public static string ToDisplayText(byte[] data)
        {
            return ProtocolEncoding.GetString(data).Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
