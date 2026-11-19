namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses
{
    // ==========================================
    // “发布命令 + 等待回执”的合并结果
    // ==========================================
    public class MqttPublishAndReceiveResult
    {
        // 命令发布结果
        public MqttPublishResult Publish { get; set; } = new MqttPublishResult();

        // 匹配到的 command-result 消息
        public MqttReceivedMessage ReceivedMessage { get; set; } = new MqttReceivedMessage();

        // 从订阅开始到收到回执的总耗时
        public long TotalElapsedMilliseconds { get; set; }
    }
}
