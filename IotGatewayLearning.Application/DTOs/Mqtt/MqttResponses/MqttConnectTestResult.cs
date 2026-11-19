namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses
{
    // ==========================================
    // 连接测试结果
    // ==========================================
    public class MqttConnectTestResult
    {
        // MQTTnet 返回的 CONNACK 结果名称，例如 Success
        public string ResultCode { get; set; } = string.Empty;

        // Broker 是否恢复了旧会话；CleanStart=true 时通常为 false
        public bool IsSessionPresent { get; set; }

        // 从开始建立连接到正常断开的总耗时
        public long ElapsedMilliseconds { get; set; }
    }
}
