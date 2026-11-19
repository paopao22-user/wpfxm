using IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests;
using IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/mqtt-diagnostics")]
    public class MqttDiagnosticsController: ControllerBase
    {
        private readonly IMqttDiagnosticService _service;

        public MqttDiagnosticsController(IMqttDiagnosticService service)
        {
            _service = service;
        }

        /// <summary>
        /// 测试 MQTT Broker 连接（需要“设备配置”权限）
        /// </summary>
        /// <param name="request">Broker 连接参数</param>
        /// <param name="cancellationToken">请求取消令牌</param>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPost("connect-test")]
        public async Task<ActionResult<ApiResponse<MqttConnectTestResult>>> ConnectTest(MqttConnectTestRequest request, CancellationToken cancellationToken)
        {
            var data = await _service.TestConnectionAsync(request, cancellationToken);

            return Ok(new ApiResponse<MqttConnectTestResult>
            {
                Code = 200,
                Message = "MQTT Broker 连接成功",
                Data = data
            });
        }

        /// <summary>
        /// 发布一条 MQTT 调试消息（需要“设备控制”权限）
        /// </summary>
        /// <param name="request">发布参数</param>
        /// <param name="cancellationToken">请求取消令牌</param>
        [Authorize(Policy = PermissionCodes.DeviceControl)]
        [HttpPost("publish")]
        public async Task<ActionResult<ApiResponse<MqttPublishResult>>> Publish(MqttPublishRequest request, CancellationToken cancellationToken)
        {
            var data = await _service.PublishAsync(request, cancellationToken);

            return Ok(new ApiResponse<MqttPublishResult>
            {
                Code = 200,
                Message = "MQTT 消息发布成功",
                Data = data
            });
        }


        /// <summary>
        /// 订阅并等待一条 MQTT 消息（需要“设备配置”权限）
        /// </summary>
        /// <param name="request">订阅参数</param>
        /// <param name="cancellationToken">请求取消令牌</param>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPost("receive-once")]
        public async Task<ActionResult<ApiResponse<MqttReceivedMessage>>> ReceiveOnce(MqttReceiveOnceRequest request, CancellationToken cancellationToken)
        {
            var data = await _service.ReceiveOnceAsync(request, cancellationToken);

            return Ok(new ApiResponse<MqttReceivedMessage>
            {
                Code = 200,
                Message = "MQTT 消息接收成功",
                Data = data
            });
        }


        /// <summary>
        /// 在同一条 MQTT 连接里：先订阅 command-result，再发布命令，最后等待匹配的回执。
        /// 同时需要“设备配置”（订阅）和“设备控制”（发布）两个权限。
        /// </summary>
        /// <param name="request">命令发布与回执订阅参数</param>
        /// <param name="cancellationToken">请求取消令牌</param>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [Authorize(Policy = PermissionCodes.DeviceControl)]
        [HttpPost("publish-and-receive")]
        public async Task<ActionResult<ApiResponse<MqttPublishAndReceiveResult>>> PublishAndReceive(MqttPublishAndReceiveRequest request, CancellationToken cancellationToken)
        {
            var data = await _service.PublishAndReceiveAsync(request, cancellationToken);

            return Ok(new ApiResponse<MqttPublishAndReceiveResult>
            {
                Code = 200,
                Message = "MQTT 命令已发布并收到回执",
                Data = data
            });
        }
    }
}
