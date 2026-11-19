using System;

namespace IotGatewayLearning.Application.DTOs.DeviceData.Responses
{
    /// <summary>
    /// 设备采样数据响应模型。
    /// 上报成功和查询最新值都复用这一个 DTO。
    /// </summary>
    public sealed class DeviceDataResponse
    {
        /// <summary>
        /// 数据库生成的采样记录主键。
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 设备业务编号。
        /// </summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>
        /// 点位业务编号。
        /// </summary>
        public string PointCode { get; set; } = string.Empty;

        /// <summary>
        /// 工程值。
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// 原始值，可能为空。
        /// </summary>
        public string? RawValue { get; set; }

        /// <summary>
        /// 单位，可能为空。
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 数据质量：Good / Bad / Uncertain。
        /// </summary>
        public string Quality { get; set; } = string.Empty;

        /// <summary>
        /// 数据来源：serial / modbus / mqtt / http / simulator / manual。
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 设备产生数据的时间，统一为 UTC。
        /// </summary>
        public DateTime OccurredAt { get; set; }

        /// <summary>
        /// 服务端接收数据的时间，统一为 UTC。
        /// </summary>
        public DateTime ReceivedAt { get; set; }
    }
}
