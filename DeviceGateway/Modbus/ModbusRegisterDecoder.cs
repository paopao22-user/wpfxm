using IotGatewayLearning.Common.Exceptions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceGateway.Modbus
{
    /// <summary>
    /// 解码器: 只负责"寄存器 → 数值"，不建立连接。
    /// </summary>
    
    public static class ModbusRegisterDecoder
    {
        /// <summary>
        /// 把 ushort 数组按数据类型和字节序解释成原始数值（还没乘 Scale）。
        /// </summary>
        /*
     需求（一句话）：给一组 PLC 返回的 ushort 寄存器、一个数据类型、一个字节序，把它解释成一个 double 原始值。

    1.ushort数组如果为空直接报502，说明上游设备plc返回的数据为空
    2.把数据类型去掉空格、转成大写
    3.如果是uint16类型，确认至少有一个寄存器，直接返回第0个
    4.如果是int16类型，确认至少有一个寄存器，把这16比特按照short来解释
    5.如果是32位类型，确认至少有2个寄存器，把字节序还原成标准的ABCD顺序
    6.如果是uint32类型，把还原后的4个字节按大端读成无符号整数
    7.如果是int32类型，把还原后的 4 个字节按大端读成有符号整数
    8.如果是float32类型，先按 int 读出 32 位比特，再按 IEEE 754 解释成浮点数
    9.如果是其他类型：报 400，并列出支持的五个类型
    10.所有分支统一返回 double，调用方不需要判断类型
     */
        public static double Decode(IReadOnlyList<ushort> registers,string dataType,string byteOrder)
        {
            if(registers == null || registers.Count == 0)
            {
                throw new AppException("PLC 返回的寄存器为空", 502);
            }

            switch (dataType.Trim().ToUpperInvariant())
            {
                case "UINT16":          // UInt16：一个寄存器，直接返回    UINT16：无符号，0 到 65535
                    RequireCount(registers, 1, "UINT16");   //RequireCount :只判断“数据够不够”，不做任何转换
                    return registers[0];        // UINT16：ushort → double
                case "INT16":           // INT16: 16位有符号整数，如果是负数，需要补码+1     INT16：有符号，最高位是符号位
                    RequireCount(registers, 1, "INT16");
                    // INT16：ushort → short → double
                    return unchecked((short)registers[0]);  //unchecked 明确告诉编译器：我要的是按位重新解释，不是数值转换.
                case "UINT32":
                    var bytesForUint32 = Normalize32(registers, byteOrder);     // Normalize32: 把 PLC 实际返回的 4 个字节，恢复成标准 ABCD 顺序。返回byte[]
                    // UINT32：byte[] → uint → double
                    return BinaryPrimitives.ReadUInt32BigEndian(bytesForUint32); //把标准大端的4字节数组按大端读成无符号整数，再转成double返回
                case "INT32":
                    var bytesForInt32 = Normalize32(registers, byteOrder);  
                    // INT32：byte[] → int → double
                    return BinaryPrimitives.ReadInt32BigEndian(bytesForInt32); //把标准大端的4字节数组按大端读成有符号整数，再转成double返回
                case "FLOAT32":
                    var bytesForFloat = Normalize32(registers, byteOrder);
                    var intBits = BinaryPrimitives.ReadInt32BigEndian(bytesForFloat); // 先按 int 读出 32 位比特
                    // FLOAT32：byte[] → int 比特 → float → double
                    return BitConverter.Int32BitsToSingle(intBits);                 // 按 IEEE 754 解释成浮点数，再转成double返回
                default:
                    throw new AppException($"不支持的数据类型：{dataType}. 支持的类型有：UINT16、INT16、UINT32、INT32、FLOAT32", 400);
            }        
        }

        /// <summary>
        /// 这个数据类型需要几个寄存器。
        /// </summary>
        public static ushort GetRequiredRegisterCount(string dataType)
        {
            switch (dataType.Trim().ToUpperInvariant())
            {
                case "UINT16":
                case "INT16":
                    return 1;

                case "UINT32":
                case "INT32":
                case "FLOAT32":
                    return 2;

                default:
                    throw new AppException($"不支持的数据类型：{dataType}", 400);
            }
        }

        private static void RequireCount(
            IReadOnlyList<ushort> registers,        // 实际拿到的寄存器
            int required,                           // 至少需要几个
            string dataType)                        // 只用于错误消息
        {
            if (registers.Count < required)
            {
                throw new AppException(
                    $"{dataType} 至少需要 {required} 个寄存器，实际只有 {registers.Count} 个",400);
            }
        }

        /// <summary>
        /// 把 PLC 实际返回的 4 个字节，恢复成标准 ABCD 顺序。
        /// </summary>
        /*
         需求： 把 PLC 实际返回的 2 个寄存器（共 4 个字节），按设备使用的字节序（ABCD / BADC / CDAB / DCBA），还原成标准大端 ABCD 顺序的 4 字节数组。

        1.先确认至少有2个寄存器，不够抛异常
        2.把寄存器0右移8位，取出高字节，记作A
        3.把寄存器0和0xFF做与运算，取出低字节，记作B
        4.把寄存器1按照同样的方式取出高字节和低字节，记作C和D
        5.把字节序字符串去掉空格、转大写，决定用哪条重排规则
        6.ABCD 原样返回；BADC 每个寄存器内部两两交换；CDAB 两个寄存器整体交换；DCBA 四个字节全部反转
        7.不认识的字节序抛400
         */
        private static byte[] Normalize32(IReadOnlyList<ushort> registers,string byteOrder)
        {
            RequireCount(registers, 2, "32位数据");

            // 每个寄存器拆成高字节 + 低字节
            var received = new byte[4]
            {
                (byte)(registers[0] >> 8),        // 高字节 A
                (byte)(registers[0] & 0xFF),      // 低字节 B
                (byte)(registers[1] >> 8),        // 高字节 C
                (byte)(registers[1] & 0xFF)        // 低字节 D
            };

            // 按配置还原成 ABCD
            switch(byteOrder.Trim().ToUpperInvariant())
            {
                case "ABCD":
                    return received;
                case "BADC":
                    return new byte[] { received[1], received[0], received[3], received[2] };
                case "CDAB":
                    return new byte[] { received[2], received[3], received[0], received[1] };
                case "DCBA":
                    return new byte[] { received[3], received[2], received[1], received[0] };
                default:
                    throw new AppException($"不支持的字节序：{byteOrder}", 400);
            }
        }
    }
}
