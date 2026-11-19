using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Telemetry
{
    /// <summary>
    /// 温度数据
    /// </summary>
    public sealed record TelemetryData
    {
        [JsonPropertyName("indoorTemperature")]
        public required double IndoorTemperature { get; init; }  // 室内温度
        
        [JsonPropertyName("outdoorTemperature")]
        public required double OutdoorTemperature { get; init; } // 室外温度

        [JsonPropertyName("targetTemperature")]
        public required double TargetTemperature { get; init; }  // 目标温度

        [JsonPropertyName("power")]
        public required bool Power { get; init; }                 // 开关机状态

        [JsonPropertyName("mode")]
        public required string Mode { get; init; }                 // 模式：制冷、制热、送风、除湿

        [JsonPropertyName("sequence")]
        public long? Sequence { get; init; } // 可选的序列号，客户端发出时生成，服务端原样返回
    }
}
