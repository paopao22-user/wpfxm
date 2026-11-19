using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Serial
{
    // ==========================================
    // 2. 串口问答响应
    // ==========================================
    public class SerialCommandResponse
    {
        // 实际发出去的字节（HEX 显示）
        public string SentHex { get; set; } = string.Empty;

        // 收到的文本（已去掉 CRLF）
        public string ReceivedText { get; set; } = string.Empty;

        // 收到的原始字节（包含 CRLF），排错用
        public string ReceivedHex { get; set; } = string.Empty;

        // 从打开端口到拿到完整一帧的耗时
        public long ElapsedMilliseconds { get; set; }

        public DateTime OccurredAt { get; set; }
    }
}
