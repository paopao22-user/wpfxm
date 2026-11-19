using System;

namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses
{
    // ==========================================
    // 订阅收到的一条消息
    // ==========================================
    public class MqttReceivedMessage
    {
        // Broker 转发过来的实际 Topic，不是订阅过滤器
        public string Topic { get; set; } = string.Empty;

        // 按 UTF-8 解码后的消息正文
        public string Payload { get; set; } = string.Empty;

        // Broker 本次转发实际使用的 QoS
        public int Qos { get; set; }

        // 是否为 Broker 保留消息
        public bool Retain { get; set; }

        // WebAPI 收到消息的 UTC 时间
        public DateTime ReceivedAt { get; set; }
    }
}
