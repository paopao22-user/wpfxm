using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Status
{
    /// <summary>
    /// 状态数据
    /// </summary>
    public sealed record StatusData
    {
        [JsonPropertyName("online")]
        public required bool Online { get; init; }  // 设备是否在线
        
        [JsonPropertyName("reason")]
        public required string Reason { get; init; }  // 离线原因：客户端发出时生成，服务端原样返回
        
        [JsonPropertyName("firmwareVersion")]
        public string? FirmwareVersion { get; init; }  // 固件版本：客户端发出时生成，服务端原样返回
    }
}
