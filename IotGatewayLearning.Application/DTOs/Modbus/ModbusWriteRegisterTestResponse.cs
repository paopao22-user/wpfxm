using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Modbus
{
    // ==========================================
    // 4. 单寄存器写入结果
    // ==========================================
    public class ModbusWriteRegisterTestResponse
    {
        // 实际写到 PLC 的 16 位原始值（250）
        public ushort WrittenRawValue { get; set; }

        // 用户请求写入的工程值（25.0）
        public double EngineeringValue { get; set; }

        public long ElapsedMilliseconds { get; set; }

        public DateTime OccurredAt { get; set; }
    }
}
