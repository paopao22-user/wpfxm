using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Devices.Responses
{
    // ==========================================
    // 3. 设备全量详情响应模型 (查看详情弹窗专用)
    // ==========================================
    /// <summary>
    /// 设备全量详情模型（包含关联的采集点位列表）
    /// </summary>
    public class DeviceDetailDto
    {
        public long Id { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public string? Location { get; set; }
        public sbyte Status { get; set; }
        public sbyte ConnectionStatus { get; set; }
        public sbyte RunStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        /// <summary>
        /// 设备下属的采集测点集合
        /// </summary>
        public List<DevicePointDto> Points { get; set; } = new();
    }
}
