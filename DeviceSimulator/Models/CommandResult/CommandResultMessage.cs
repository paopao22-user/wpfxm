using IotGatewayLearning.DeviceSimulator.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.CommandResult
{
    /// <summary>
    /// 命令结果消息
    /// </summary>
    public sealed record CommandResultMessage:MqttMessage
    {
        [JsonPropertyName("data")]
        public required CommandResultData Data { get; init; }  // 业务数据
    }
}
