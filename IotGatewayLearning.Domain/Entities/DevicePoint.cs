using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备点位实体，对应数据库 device_points 表。
    /// 一个点位 = 设备上一个可读取/可写入的数据位置，
    /// 它同时是"采集说明书"：告诉采集器去哪儿读、怎么解释、什么单位。
    /// </summary>
    [Table("device_points")]
    public class DevicePoint
    {
        // ==============================
        // 主键
        // ==============================
        [Key]
        [Column("id")]
        public long Id { get; set; }


        // ==============================
        // 外键：这个点位属于哪台设备
        // 对应数据库外键 fk_device_points_device
        // ==============================
        [Column("device_id")]
        public long DeviceId { get; set; }

        // ==============================
        // 点位编码：程序内部稳定使用的名字
        // 例如 temperature、pressure、run_status
        // 程序里不要写中文名称，也不要直接写寄存器号
        // 同一台设备内唯一（device_id + point_code 复合唯一）
        // ==============================
        [Required]
        [MaxLength(80)]
        [Column("point_code")]
        public string PointCode { get; set; } = string.Empty;

        // ==============================
        // 点位名称：给页面显示的中文名，例如"排气温度"
        // ==============================
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        // ==============================
        // 协议地址
        // 故意用字符串，因为不同协议写法完全不同：
        //   Modbus → 40001
        //   MQTT   → factory/line1/ac001/temperature
        //   OPC UA → ns=2;s=Device1.Temperature
        // 具体怎么解释由所属设备的 protocol 决定
        // ==============================
        [Required]
        [MaxLength(255)]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        // ==============================
        // 数据类型：int / decimal / bool / string
        // 决定采集回来的原始字节怎么解释
        // ==============================
        [Required]
        [MaxLength(30)]
        [Column("data_type")]
        public string DataType { get; set; } = string.Empty;

        // ==============================
        // 数据单位：℃ / MPa / A / %
        // 状态类点位没有单位，所以允许 null
        // 82.5 没有单位时，用户不知道它是温度还是压力
        // ==============================
        [MaxLength(30)]
        [Column("unit")]
        public string? Unit { get; set; }

        // ==============================
        // 缩放系数：真实值 = 原始值 × Scale + OffsetValue
        // 例：寄存器 825，Scale=0.1 → 82.5 ℃
        // 默认 1m 表示不缩放（注意是 1，不是 0）
        // TypeName 显式写出精度，和数据库 decimal(12,4) 对齐
        // ==============================
        [Column("scale", TypeName = "decimal(12,4)")]
        public decimal Scale { get; set; } = 1m;

        // ==============================
        // 偏移量：用于设备校准后的修正值
        // 默认 0m 表示不偏移
        // ==============================
        [Column("offset_value", TypeName = "decimal(12,4)")]
        public decimal OffsetValue { get; set; }

        // ==============================
        // 访问模式：read=只读，write=只写，read_write=可读可写
        // 防止只读点位被误写
        // ==============================
        [Required]
        [MaxLength(20)]
        [Column("access_mode")]
        public string AccessMode { get; set; } = "read";

        // ==============================
        // 是否启用：1=参与采集，0=暂时停用
        // 停用点位不等于删除点位，配置还在
        // 同样用 sbyte 对应 MySQL 的有符号 tinyint
        // ==============================
        [Column("enabled")]
        public sbyte Enabled { get; set; } = 1;

        // ==============================
        // 创建时间 / 修改时间
        // 同 Device：交给数据库维护，不写 C# 默认值
        // ==============================
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // ==============================
        // 导航属性：从点位回到所属设备（多对一的反向端）
        // 可空，因为不是每次查询都会加载它
        // ==============================
        public Device? Device { get; set; }
    }
}
