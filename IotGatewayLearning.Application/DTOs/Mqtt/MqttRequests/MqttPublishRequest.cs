using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests
{
    // ==========================================
    // 发布一条调试消息请求
    // ==========================================
    public class MqttPublishRequest : MqttBrokerRequest
    {
        // 发布目标 Topic，必须是明确 Topic，不能含 + 或 #
        [Required(ErrorMessage = "topic 不能为空")]
        public string Topic { get; set; } = string.Empty;

        // UTF-8 文本载荷；允许空字符串，可用于清除 retained 消息
        [Required(AllowEmptyStrings = true, ErrorMessage = "payload 不能为 null")]
        public string Payload { get; set; } = string.Empty;

        // QoS 只允许 0、1、2
        [Range(0, 2, ErrorMessage = "qos 只能是 0、1 或 2")]
        public int Qos { get; set; } = 1;

        // 是否保留为该 Topic 的最后一条消息
        public bool Retain { get; set; }
    }
}
