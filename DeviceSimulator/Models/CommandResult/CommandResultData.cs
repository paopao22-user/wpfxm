using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.CommandResult
{
    /// <summary>
    /// 命令结果数据
    /// </summary>
    public sealed record CommandResultData
    {
        [JsonPropertyName("commandId")]
        public required string CommandId { get; init; }  // 命令ID

        [JsonPropertyName("success")]
        public required bool Success { get; init; }  // 命令执行结果

        [JsonPropertyName("code")]
        public required string Code { get; init; }  // 命令执行结果码
        
        [JsonPropertyName("message")]
        public string? Message { get; init; }  // 命令执行结果描述

        [JsonPropertyName("completedAt")]
        public DateTimeOffset? CompletedAt { get; init; }  // 命令完成时间，客户端发出时生成，服务端原样返回
    }
}
