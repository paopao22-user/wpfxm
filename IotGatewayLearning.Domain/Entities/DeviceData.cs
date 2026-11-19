using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备时序采样实体，对应数据库 device_data 表。
    /// 一条记录 = 一台设备的一个点位在某一个时刻的一次采样。
    /// </summary>
    [Table("device_data")]
    public class DeviceData
    {
        // ==============================
        // 主键
        // ==============================
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // ==============================
        // 设备业务编号
        // 这里保存的是上报时的编号快照，不建立外键。
        // 即使以后设备改名或停用，历史记录仍然可以追溯。
        // ==============================
        [Required]
        [MaxLength(50)]
        [Column("device_code")]
        public string DeviceCode { get; set; } = string.Empty;

        // ==============================
        // 点位业务编号
        // 同一台设备内点位编码唯一，因此 device_code + point_code
        // 可以定位到一个具体点位。
        // ==============================
        [Required]
        [MaxLength(80)]
        [Column("point_code")]
        public string PointCode { get; set; } = string.Empty;

        // ==============================
        // 工程值
        // 页面曲线和卡片直接使用这个值，不在这里做原始值换算。
        // 显式写出 decimal(20,6)，与数据库表结构保持一致。
        // ==============================
        [Column("value_decimal", TypeName = "decimal(20,6)")]
        public decimal ValueDecimal { get; set; }

        // ==============================
        // 原始值
        // 可能是寄存器值、十六进制报文或文本，因此使用字符串保存。
        // 它的作用是排查“协议解析或倍率换算”问题。
        // ==============================
        [MaxLength(255)]
        [Column("raw_value")]
        public string? RawValue { get; set; }

        // ==============================
        // 单位
        // 允许为空，因为状态类点位可能没有单位。
        // 上报时如果客户端没有传单位，Service 会尝试使用点位配置的单位。
        // ==============================
        [MaxLength(30)]
        [Column("unit")]
        public string? Unit { get; set; }

        // ==============================
        // 数据质量
        // 只允许 Good / Bad / Uncertain，具体校验由 Service 负责。
        // ==============================
        [Required]
        [MaxLength(32)]
        [Column("quality")]
        public string Quality { get; set; } = "Good";

        // ==============================
        // 数据来源
        // serial / modbus / mqtt / http / simulator / manual
        // 表示数据来源类别，不是具体协议配置字符串。
        // ==============================
        [Required]
        [MaxLength(32)]
        [Column("source")]
        public string Source { get; set; } = string.Empty;

        // ==============================
        // 设备产生数据的时间
        // 由设备上报，Service 统一转换为 UTC 后保存。
        // ==============================
        [Column("occurred_at", TypeName = "datetime(3)")]
        public DateTime OccurredAt { get; set; }

        // ==============================
        // 服务端接收请求的时间
        // 由服务端生成，不能相信客户端传入的值。
        // ==============================
        [Column("received_at", TypeName = "datetime(3)")]
        public DateTime ReceivedAt { get; set; }

        // ==============================
        // 数据库写入时间
        // 阶段一由 Service 赋值，保留数据库默认值作为兜底。
        // ==============================
        [Column("created_at", TypeName = "datetime(3)")]
        public DateTime CreatedAt { get; set; }
    }
}
