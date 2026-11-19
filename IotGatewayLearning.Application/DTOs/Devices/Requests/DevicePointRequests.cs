using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Devices.Requests
{
    // ==========================================
    // 1. 新增点位请求模型
    // ==========================================
    /// <summary>
    /// 新增设备采集点位时客户端提交的参数载荷
    /// </summary>
    public class CreateDevicePointRequest
    {
        /// <summary>
        /// 点位业务编码（同一设备内唯一，英文/拼音标识，如 temperature）
        /// </summary>
        [Required]
        [MaxLength(80)]
        public string PointCode { get; set; } = string.Empty;

        /// <summary>
        /// 点位显示名称（给操作员看的中文名，如 排气温度）
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 协议物理地址（Modbus 寄存器如 40001，MQTT Topic 如 line1/temp）
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 原始值解析数据类型（如 int16, float, decimal, bool）
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string DataType { get; set; } = "decimal";

        /// <summary>
        /// 工程单位（如 ℃, MPa, RPM；开关量状态点位可为空）
        /// </summary>
        [MaxLength(30)]
        public string? Unit { get; set; }


        /// <summary>
        /// 缩放倍率（实际值 = 原始值 × Scale + OffsetValue；默认 1 表示不缩放）
        /// </summary>
        public decimal Scale { get; set; } = 1.0m;

        /// <summary>
        /// 校准偏移量（零点漂移修正值；默认 0 表示不偏移）
        /// </summary>
        public decimal OffsetValue { get; set; } = 0.0m;

        /// <summary>
        /// 访问权限方向（read: 只读, write: 只写, readwrite: 读写）
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string AccessMode { get; set; } = "read";

        /// <summary>
        /// 是否启用采集（1: 启用, 0: 停用）
        /// </summary>
        [Range(0, 1)]
        public sbyte Enabled { get; set; } = 1;
    }


    // ==========================================
    // 2. 修改点位请求模型
    // ==========================================
    /// <summary>
    /// 修改设备点位时提交的参数载荷（全量覆盖提交，字段与新增保持一致）
    /// </summary>
    public sealed class UpdateDevicePointRequest : CreateDevicePointRequest
    {

    }
}
