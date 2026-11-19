using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Models.Common
{
    /// <summary>
    /// 所有 MQTT 业务消息共用的信封。          信封基类
    /// 信封负责“谁、什么时候、什么类型、按哪个合同版本”，
    /// 业务字段统一放在派生类的 Data 中。
    /// </summary>
    public abstract record MqttMessage
    {
        [JsonPropertyName("messageId")]
        public required string MessageId { get; init; }       // 消息 ID：GUID，客户端发出时生成，服务端原样返回 }

        [JsonPropertyName("deviceCode")]
        public required string DeviceCode { get; init; }      // 设备编号：客户端发出时生成，服务端原样返回 }

        [JsonPropertyName("messageType")]
        public required string MessageType { get; init; }      // 消息类型：客户端发出时生成，服务端原样返回  

        [JsonPropertyName("timestamp")]
        public required DateTimeOffset Timestamp { get; init; }    // 时间戳：客户端发出时生成，服务端原样返回

        [JsonPropertyName("schemaVersion")]
        public required string SchemaVersion { get; init; }   // 合同版本：客户端发出时生成，服务端原样返回


    }
}
