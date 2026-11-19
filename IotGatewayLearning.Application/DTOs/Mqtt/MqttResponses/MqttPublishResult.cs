namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses
{
    // ==========================================
    // 发布结果
    // ==========================================
    public class MqttPublishResult
    {
        // 实际发布的 Topic
        public string Topic { get; set; } = string.Empty;

        // 实际使用的 QoS
        public int Qos { get; set; }

        // 实际使用的 Retain 标记
        public bool Retain { get; set; }

        // MQTTnet 返回的发布结果名称，例如 Success
        public string ReasonCode { get; set; } = string.Empty;

        // 从开始建立连接到发布完成并断开的总耗时
        public long ElapsedMilliseconds { get; set; }
    }
}
