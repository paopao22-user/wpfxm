using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Modbus
{
    // ==========================================
    // 3. 单寄存器写入请求
    // ==========================================
    public class ModbusWriteRegisterTestRequest
    {
        [Required(ErrorMessage = "host 不能为空")]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 1502;

        [Range(1, 247)]
        public byte UnitId { get; set; } = 1;

        // 要写的零基保持寄存器地址
        [Range(0, 65535)]
        public ushort ProtocolAddress { get; set; }

        // 页面输入的工程值，例如 25.0
        public double EngineeringValue { get; set; }

        [Required]
        public string DataType { get; set; } = "UInt16";

        public double Scale { get; set; } = 1;

        public double Offset { get; set; }

        [Range(100, 30000)]
        public int ConnectTimeoutMs { get; set; } = 3000;

        [Range(100, 30000)]
        public int ReadTimeoutMs { get; set; } = 2000;

        // ⚠️ 写操作必须明确确认，防止误触
        public bool Confirmed { get; set; }
    }
}
