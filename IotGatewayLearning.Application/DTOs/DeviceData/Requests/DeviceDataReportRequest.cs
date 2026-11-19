using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs.DeviceData.Requests
{
    public sealed class DeviceDataReportRequest
    {
        [Required, StringLength(50)]
        public string DeviceCode { get; set; } = string.Empty;      // 设备编号

        [Required, StringLength(80)]
        public string PointCode { get; set; } = string.Empty;       // 点位编号


        [Required]
        public decimal Value { get; set; }      // 点位值

        [StringLength(255)]
        public string? RawValue { get; set; }       // 原始值

        [StringLength(30)]
        public string? Unit { get; set; }       // 单位

        [Required]
        public string Quality { get; set; } = string.Empty;     // 数据质量

        [Required]
        public string Source { get; set; } = string.Empty;      // 数据来源

        [Required]
        public DateTime OccurredAt { get; set; }            // 数据发生时间
    }
}
