using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Command
{
    /// <summary>
    /// 命令数据
    /// </summary>
    public sealed record CommandData
    {
        [JsonPropertyName("action")]
        public required string Action { get; init; }  // 命令动作

        [JsonPropertyName("parameters")]
        public JsonObject? Parameters { get; init; }  // 可选的命令参数

        [JsonPropertyName("expiresAt")]
        public DateTimeOffset? ExpiresAt { get; init; }  // 可选的命令过期时间，客户端发出时生成，服务端原样返回
    }
}
