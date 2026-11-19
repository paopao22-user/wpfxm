using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.Devices.Responses
{
    // ==========================================
    // 2. 采集点位响应模型 (测点子表专用)
    // ==========================================
    /// <summary>
    /// 采集测点数据项模型
    /// </summary>
    public class DevicePointDto
    {
        public long Id { get; set; }
        public long DeviceId { get; set; }
        public string PointCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Scale { get; set; }
        public decimal OffsetValue { get; set; }
        public string AccessMode { get; set; } = string.Empty;
        public sbyte Enabled { get; set; }
    }
}
