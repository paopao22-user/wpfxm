using IotGatewayLearning.DeviceSimulator.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Telemetry
{
    /// <summary>
    /// 遥测消息
    /// </summary>
    public sealed record TelemetryMessage:MqttMessage
    {
        [JsonPropertyName("data")]
        public required TelemetryData Data { get; init; }
    }
}
