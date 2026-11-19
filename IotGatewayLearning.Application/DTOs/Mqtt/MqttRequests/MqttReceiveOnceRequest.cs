using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests
{
    // ==========================================
    // 订阅并等待一条消息请求
    // ==========================================
    public class MqttReceiveOnceRequest : MqttBrokerRequest
    {
        // 订阅过滤器，允许 + 和 #
        [Required(ErrorMessage = "topicFilter 不能为空")]
        public string TopicFilter { get; set; } = string.Empty;

        // 订阅要求的最大 QoS，只允许 0、1、2
        [Range(0, 2, ErrorMessage = "qos 只能是 0、1 或 2")]
        public int Qos { get; set; } = 1;

        // 订阅成功后等待消息的最大时间
        [Range(500, 60000, ErrorMessage = "waitTimeoutMilliseconds 必须在 500 到 60000 之间")]
        public int WaitTimeoutMilliseconds { get; set; } = 10000;
    }
}
