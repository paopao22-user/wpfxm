using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Devices.Requests
{
    // ==========================================
    // 1. 新增设备请求契约
    // ==========================================
    /// <summary>
    /// 新增设备时客户端提交的请求参数
    /// </summary>
    public class CreateDeviceRequest
    {
        [Required]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

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
