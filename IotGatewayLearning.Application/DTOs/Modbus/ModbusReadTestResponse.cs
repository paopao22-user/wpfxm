using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Modbus
{
    // ==========================================
    // 2. 单次读取结果
    // ==========================================
    public class ModbusReadTestResponse
    {
        // PLC 返回的原始寄存器（先看它，判断通信对不对）
        public ushort[] Registers { get; set; } = Array.Empty<ushort>();

        // 按 dataType/byteOrder 解释后的原始数值
        public double RawValue { get; set; }

        // 再乘 Scale 加 Offset 后的工程值
        public double EngineeringValue { get; set; }

        public string Quality { get; set; } = "Good";

        public long ElapsedMilliseconds { get; set; }

        public DateTime OccurredAt { get; set; }
    }

}
