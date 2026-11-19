using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests
{
    // ==========================================
    // 三个诊断请求共用的 Broker 连接参数
    // ==========================================
    public abstract class MqttBrokerRequest
    {
        // Broker 主机名或 IP，不包含 mqtt:// 前缀
        [Required(ErrorMessage = "host 不能为空")]
        public string Host { get; set; } = "127.0.0.1";

        // MQTT TCP 端口，本地 EMQX 默认 1883
        [Range(1, 65535, ErrorMessage = "port 必须在 1 到 65535 之间")]
        public int Port { get; set; } = 1883;

        // 本次诊断连接的唯一 ClientId；重复会踢掉旧连接
        [Required(ErrorMessage = "clientId 不能为空")]
        public string ClientId { get; set; } = string.Empty;

        // 认证用户名；本地匿名模式允许为空
        public string? Username { get; set; }

        // 认证密码；只参与本次请求，禁止写入日志
        public string? Password { get; set; }

        // 建立 TCP + MQTT 连接的最大等待时间
        [Range(500, 120000, ErrorMessage = "connectTimeoutMilliseconds 必须在 500 到 120000 之间")]
        public int ConnectTimeoutMilliseconds { get; set; } = 5000;
    }
}
