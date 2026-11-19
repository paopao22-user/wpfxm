using IotGatewayLearning.DeviceSimulator.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Status
{
    /// <summary>
    /// 状态消息
    /// </summary>
    public sealed record StatusMessage:MqttMessage
    {
        [JsonPropertyName("data")]
        public required StatusData Data { get; init; }  // 业务数据}
    }
}
