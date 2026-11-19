using System.Collections.Generic;

namespace IotGatewayLearning.Application.DTOs.Mqtt
{
    // ==========================================
    // Broker 主机白名单
    // ==========================================
    public class MqttDiagnosticsOptions
    {
        // 默认只允许本机 EMQX，防止诊断接口被用来访问任意内网地址
        public List<string> AllowedHosts { get; set; } = new List<string>
        {
            "127.0.0.1",
            "localhost",
            "::1"
        };
    }
}
