using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Serial
{
    // ==========================================
    // 1. 串口问答请求
    // ==========================================
    public class SerialCommandRequest
    {
        // WebAPI 所在电脑的串口名，例如 COM11
        [Required(ErrorMessage = "portName 不能为空")]
        public string PortName { get; set; } = "COM11";

        [Range(50, 4000000, ErrorMessage = "baudRate 超出合理范围")]
        public int BaudRate { get; set; } = 9600;

        // 要发送的文本（不含结束符，结束符由 Terminator 自动追加）
        [Required(ErrorMessage = "payload 不能为空")]
        public string Payload { get; set; } = string.Empty;

        // None / CR / LF / CRLF
        [Required]
        public string Terminator { get; set; } = "CRLF";

        [Range(100, 30000, ErrorMessage = "readTimeoutMs 必须在 100 到 30000 之间")]
        public int ReadTimeoutMs { get; set; } = 2000;
    }
}
