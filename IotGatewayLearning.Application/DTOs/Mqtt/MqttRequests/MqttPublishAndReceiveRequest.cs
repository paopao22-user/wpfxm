using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests
{
    // ==========================================
    // 在【同一条 MQTT 连接】里先订阅回执，再发布命令。
    //
    // 为什么需要它：
    //   publish 和 receive-once 是两个独立的 HTTP 请求，各自创建临时 MQTT 连接。
    //   如果前端先发 receive-once、等 600ms 再 publish，Broker 慢一点时
    //   订阅还没生效，命令回执就已经发出来了，回执会永久丢失。
    //   这个接口把“订阅 + 发布 + 等回执”放在一个连接内完成，从根上消除竞态。
    // ==========================================
    public class MqttPublishAndReceiveRequest : MqttBrokerRequest
    {
        // 命令发布目标 Topic，必须是明确 Topic，不能含 + 或 #
        [Required(ErrorMessage = "commandTopic 不能为空")]
        public string CommandTopic { get; set; } = string.Empty;

        // 命令正文；允许空字符串，但通常是一段 JSON
        [Required(AllowEmptyStrings = true, ErrorMessage = "commandPayload 不能为 null")]
        public string CommandPayload { get; set; } = string.Empty;

        [Range(0, 2, ErrorMessage = "commandQos 只能是 0、1 或 2")]
        public int CommandQos { get; set; } = 1;

        public bool CommandRetain { get; set; }

        // 命令回执订阅 Topic，必须是明确 Topic
        [Required(ErrorMessage = "resultTopic 不能为空")]
        public string ResultTopic { get; set; } = string.Empty;

        [Range(0, 2, ErrorMessage = "resultQos 只能是 0、1 或 2")]
        public int ResultQos { get; set; } = 1;

        // 发布命令后等待匹配回执的最大时间
        [Range(500, 60000, ErrorMessage = "waitTimeoutMilliseconds 必须在 500 到 60000 之间")]
        public int WaitTimeoutMilliseconds { get; set; } = 10000;

        // 只接受 Payload 中包含该字符串的回执，通常传 commandId。
        // 用来过滤掉同 Topic 上其他命令的回执，避免“张冠李戴”。
        [StringLength(256, ErrorMessage = "resultPayloadContains 长度不能超过 256")]
        public string? ResultPayloadContains { get; set; }
    }
}
