using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备台账实体，对应数据库 devices 表。
    /// 一条记录 = 一台被管理的设备（PLC、空压机、空调、传感器……）
    /// </summary>
    [Table("devices")]
    public class Device
    {
        // ==============================
        // 主键
        // ==============================
        [Key]
        [Column("id")]
        public long  Id { get; set; }


        // ==============================
        // 设备编号（业务唯一标识）
        // 现场贴的二维码、工单、报表都靠它认设备
        // 对应数据库唯一索引 uk_devices_device_code
        // ==============================
        [Required]
        [MaxLength(50)]
        [Column("device_code")]
        public string DeviceCode { get; set; } = string.Empty;


        // ==============================
        // 设备名称
        // ==============================
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;


        // ==============================
        // 设备类型：PLC / AirConditioner / Sensor ……
        // ==============================
        [Required]
        [MaxLength(50)]
        [Column("device_type")]
        public string DeviceType { get; set; } = string.Empty;


        // ==============================
        // 通信协议：modbus-tcp / mqtt / simulator ……
        // 注意：这只是"打算用什么协议连它"，不代表已经实现了该协议
        // ==============================
        [Required]
        [MaxLength(30)]
        [Column("protocol")]
        public string Protocol { get; set; } = string.Empty;


        // ==============================
        // IP 地址或主机名
        // MQTT、串口、模拟器类设备没有 IP，所以允许 null
        // ==============================
        [MaxLength(100)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        // ==============================
        // 通信端口（只有 TCP 类设备才需要）
        // 用 int? 而不是 int，是为了如实表达"这台设备没有端口"
        // ==============================
        [Column("port")]
        public int? Port { get; set; }


        // ==============================
        // 安装位置
        // ==============================
        [MaxLength(200)]
        [Column("location")]
        public string? Location { get; set; }


        // ==============================
        // 台账状态：1=启用，0=停用
        // 由"人"维护（管理员在页面上点停用），和连接状态是两回事
        //
        // 为什么用 sbyte 而不是 byte？
        // MySQL 的 tinyint 是有符号的，Pomelo 的映射规则是：
        //   sbyte ↔ tinyint（有符号）
        //   byte  ↔ tinyint unsigned（无符号）
        // 本项目其他实体（User.Status、Permission.Status）都用 sbyte，
        // 用 byte 会让 EF 认为列是无符号的，将来生成多余的 ALTER 语句
        // ==============================
        [Column("status")]
        public sbyte Status { get; set; } = 1;

        // ==============================
        // 软删除标记：0=未删除，1=已删除
        // 业务上的"删除设备"= 把它改成 1，不做物理 DELETE
        // 所以所有列表查询都必须带 is_deleted = 0
        // ==============================
        [Column("is_deleted")]
        public sbyte IsDeleted { get; set; }

        // ==============================
        // 连接状态：0=未知，1=在线，2=离线，3=故障
        // 由后续采集服务维护，台账页面只读
        // ==============================
        [Column("connection_status")]
        public sbyte ConnectionStatus { get; set; }

        // ==============================
        // 运行状态：0=停机，1=运行，2=待机，3=维修，4=报废
        // 同样由后续采集/控制服务维护
        // ==============================
        [Column("run_status")]
        public sbyte RunStatus { get; set; }


        // ==============================
        // 创建时间
        // 故意不写 = DateTime.UtcNow：
        // 时间由数据库的 DEFAULT CURRENT_TIMESTAMP 负责填，
        // 这样"程序插入的行"和"SQL 直接插入的行"时间基准一致，
        // 不会出现一行里 created_at 是 UTC、updated_at 是本地时间的情况
        // ==============================
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }


        // ==============================
        // 修改时间
        // 由数据库的 ON UPDATE CURRENT_TIMESTAMP 自动维护
        // ==============================
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // ==============================
        // 导航属性：一台设备拥有多个点位（一对多）
        // 初始化成空集合，避免还没加载时就 NullReferenceException
        // ==============================
        public ICollection<DevicePoint> Points { get; set; } = new List<DevicePoint>();
    }
}
