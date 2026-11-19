using IotGatewayLearning.DeviceSimulator.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Command
{
    /// <summary>
    /// 命令消息
    /// </summary>
    public sealed record CommandMessage:MqttMessage
    {
        [JsonPropertyName("data")]
        public required CommandData Data { get; init; }  // 业务数据
    }
}
