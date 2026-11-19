using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Devices.Requests
{
    /// <summary>
    /// 更新设备请求
    /// </summary>
    public class UpdateDeviceRequest
    {
        // 允许修改业务设备编号，但仍然必须保持唯一。
        [Required]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        // 设备名称。
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        //  设备类型。
        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Protocol { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [Range(1, 65535)]
        public int? Port { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }   
    }
}
