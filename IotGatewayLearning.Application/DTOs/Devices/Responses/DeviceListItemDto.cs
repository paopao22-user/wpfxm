using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Devices.Responses
{
    // ==========================================
    // 1. 设备列表项响应模型 (分页表格专用)
    // ==========================================
    /// <summary>
    /// 设备台账分页列表展示的轻量模型（不包含测点详情）
    /// </summary>
    public class DeviceListItemDto
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
    }
}
