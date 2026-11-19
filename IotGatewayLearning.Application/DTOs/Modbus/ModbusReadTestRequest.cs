using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Modbus
{
    // ==========================================
    // 1. 单次读取测试请求
    // ==========================================
    public class ModbusReadTestRequest
    {
        // PLC 的 IP 或主机名
        [Required(ErrorMessage = "host 不能为空")]
        public string Host { get; set; } = string.Empty;

        // Modbus TCP 端口：真实 PLC 是 502，实验用 1502
        [Range(1, 65535, ErrorMessage = "port 必须在 1 到 65535 之间")]
        public int Port { get; set; } = 1502;

        // 站号（TCP 里叫 Unit Id）
        [Range(1, 247, ErrorMessage = "unitId 必须在 1 到 247 之间")]
        public byte UnitId { get; set; } = 1;

        // holding（功能码03）或 input（功能码04）
        [Required(ErrorMessage = "registerArea 不能为空")]
        public string RegisterArea { get; set; } = "holding";

        // ⚠️ 零基协议地址，不是手册编号 40001
        [Range(0, 65535, ErrorMessage = "protocolAddress 超出范围")]
        public ushort ProtocolAddress { get; set; }

        // 连续读几个 16 位寄存器
        [Range(1, 4, ErrorMessage = "registerCount 必须在 1 到 4 之间")]
        public ushort RegisterCount { get; set; } = 1;

        // UInt16 / Int16 / UInt32 / Int32 / Float32
        [Required(ErrorMessage = "dataType 不能为空")]
        public string DataType { get; set; } = "Int16";

        // 32 位数据的字节序：ABCD / BADC / CDAB / DCBA
        [Required(ErrorMessage = "byteOrder 不能为空")]
        public string ByteOrder { get; set; } = "ABCD";

        // 原始值 × Scale + Offset = 工程值
        public double Scale { get; set; } = 1;

        public double Offset { get; set; }

        [Range(100, 30000)]
        public int ConnectTimeoutMs { get; set; } = 3000;

        [Range(100, 30000)]
        public int ReadTimeoutMs { get; set; } = 2000;
    }
}
