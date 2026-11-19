using IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests;
using IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// MQTT 诊断能力。
    /// Application 只声明"要什么能力"，
    /// 具体怎么连接 Broker、怎么发布订阅，由 DeviceGateway 实现。
    /// </summary>
    public interface IMqttDiagnosticService
    {
        // 连接 Broker，读取 CONNACK 后立即正常断开
        Task<MqttConnectTestResult> TestConnectionAsync(MqttConnectTestRequest request, CancellationToken cancellationToken = default);

        // 建立临时连接并发布一条消息
        Task<MqttPublishResult> PublishAsync(MqttPublishRequest request, CancellationToken cancellationToken = default);

        // 建立临时连接，订阅后等待第一条匹配消息
        Task<MqttReceivedMessage> ReceiveOnceAsync(MqttReceiveOnceRequest request, CancellationToken cancellationToken = default);

        // 在【同一条连接】里先订阅回执，再发布命令，最后等待匹配的回执。
        // 用于消除 publish 与 receive-once 分两次 HTTP 请求时的订阅竞态。
        Task<MqttPublishAndReceiveResult> PublishAndReceiveAsync(MqttPublishAndReceiveRequest request, CancellationToken cancellationToken = default);
    }
}
