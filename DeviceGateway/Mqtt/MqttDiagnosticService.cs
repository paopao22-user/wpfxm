using IotGatewayLearning.Application.DTOs.Mqtt;
using IotGatewayLearning.Application.DTOs.Mqtt.MqttRequests;
using IotGatewayLearning.Application.DTOs.Mqtt.MqttResponses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceGateway.Mqtt
{
    public sealed class MqttDiagnosticService : IMqttDiagnosticService
    {
        // 工厂本身无状态，一个服务实例持有一个即可。
        private readonly MqttClientFactory _factory = new();

        // 主机白名单，防止诊断接口被用来访问任意内网地址。
        private readonly HashSet<string> _allowedHosts;

        // 构造函数：从配置读取白名单
        public MqttDiagnosticService(MqttDiagnosticsOptions options)
        {
            _allowedHosts = new HashSet<string>(options.AllowedHosts ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }


        // ============================================================
        // 1. 连接测试：连接 → 读取 CONNACK → 断开
        // ============================================================
        public Task<MqttConnectTestResult> TestConnectionAsync(MqttConnectTestRequest request, CancellationToken cancellationToken = default)
        {
            ValidateBroker(request);

            var stopwatch = Stopwatch.StartNew();

            return ExecuteAsync(
                request,
                (_, connectResult, _) =>
                {
                    stopwatch.Stop();

                    return Task.FromResult(new MqttConnectTestResult
                    {
                        ResultCode = connectResult.ResultCode.ToString(),
                        IsSessionPresent = connectResult.IsSessionPresent,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    });
                },
                cancellationToken);
        }

        // ============================================================
        // 2. 发布：连接 → 构造消息 → PublishAsync → 断开
        // ============================================================
        public Task<MqttPublishResult> PublishAsync(MqttPublishRequest request, CancellationToken cancellationToken = default)
        {
            ValidateBroker(request);
            ValidatePublish(request);

            var topic = request.Topic.Trim();
            var qos = (MqttQualityOfServiceLevel)request.Qos;
            var payload = request.Payload ?? string.Empty;
            var stopwatch = Stopwatch.StartNew();

            return ExecuteAsync(
                request,
                async (client, _, token) =>
                {
                    var message = _factory
                        .CreateApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(qos)
                        .WithRetainFlag(request.Retain)
                        .Build();

                    var publishResult = await client.PublishAsync(message, token);

                    // NoMatchingSubscribers 只表示当前没有订阅者，
                    // 不是发布失败，诊断接口应把 ReasonCode 原样返回。
                    if (!publishResult.IsSuccess &&
                        publishResult.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers)
                    {
                        throw new AppException(
                            $"MQTT 发布失败：{publishResult.ReasonCode}",
                            502);
                    }

                    stopwatch.Stop();

                    return new MqttPublishResult
                    {
                        Topic = topic,
                        Qos = request.Qos,
                        Retain = request.Retain,
                        ReasonCode = publishResult.ReasonCode.ToString(),
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                },
                cancellationToken);
        }

        // ============================================================
        // 3. 订阅一次：注册回调 → 连接 → 订阅 → 等第一条 → 断开
        // ============================================================
        public Task<MqttReceivedMessage> ReceiveOnceAsync(MqttReceiveOnceRequest request, CancellationToken cancellationToken = default)
        {
            ValidateBroker(request);
            ValidateReceive(request);

            var completion = new TaskCompletionSource<MqttReceivedMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            return ExecuteAsync(
                request,
                async (client, _, token) =>
                {
                    var subscribeOptions = _factory
                        .CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(filter => filter
                            .WithTopic(request.TopicFilter.Trim())
                            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)request.Qos))
                        .Build();

                    var subscribeResult = await client.SubscribeAsync(
                        subscribeOptions,
                        token);

                    EnsureSubscriptionGranted(subscribeResult);

                    try
                    {
                        return await completion.Task.WaitAsync(
                            TimeSpan.FromMilliseconds(request.WaitTimeoutMilliseconds),
                            token);
                    }
                    catch (TimeoutException)
                    {
                        throw new AppException(
                            $"订阅成功，但 {request.WaitTimeoutMilliseconds} ms 内没有收到消息",
                            504);
                    }
                },
                cancellationToken,
                client =>
                {
                    // 必须在 Connect / Subscribe 之前注册，
                    // 避免订阅成功后第一条消息到达时还没有处理器。
                    client.ApplicationMessageReceivedAsync += eventArgs =>
                    {
                        var message = eventArgs.ApplicationMessage;

                        completion.TrySetResult(new MqttReceivedMessage
                        {
                            Topic = message.Topic,
                            Payload = message.ConvertPayloadToString(),
                            Qos = (int)message.QualityOfServiceLevel,
                            Retain = message.Retain,
                            ReceivedAt = DateTime.UtcNow
                        });

                        return Task.CompletedTask;
                    };
                });
        }


        // ============================================================
        // 4. 发布命令并等待回执：订阅 → 发布 → 等匹配消息 → 断开
        //
        // 和“先调 receive-once，再调 publish”相比，这里只有一个 MQTT 客户端：
        //   SubscribeAsync 完成 → 订阅已经在 Broker 生效 → 才 PublishAsync。
        // 因此不会出现“回执已经发出，但订阅还没建立”的竞态。
        // ============================================================
        public Task<MqttPublishAndReceiveResult> PublishAndReceiveAsync(
            MqttPublishAndReceiveRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateBroker(request);
            ValidatePublishAndReceive(request);

            var commandTopic = request.CommandTopic?.Trim() ?? string.Empty;
            var resultTopic = request.ResultTopic?.Trim() ?? string.Empty;
            var commandPayload = request.CommandPayload ?? string.Empty;
            var payloadMarker = string.IsNullOrWhiteSpace(request.ResultPayloadContains)
                ? null
                : request.ResultPayloadContains.Trim();

            var completion = new TaskCompletionSource<MqttReceivedMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var totalStopwatch = Stopwatch.StartNew();

            // 订阅刚建立时，Broker 可能补发该 Topic 上的 retained 旧消息。
            // 命令还没发布之前收到的消息一律不认，避免拿到上一条 command-result。
            var acceptMessages = 0;

            return ExecuteAsync(
                request,
                async (client, _, token) =>
                {
                    var subscribeOptions = _factory
                        .CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(filter => filter
                            .WithTopic(resultTopic)
                            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)request.ResultQos))
                        .Build();

                    var subscribeResult = await client.SubscribeAsync(
                        subscribeOptions,
                        token);

                    EnsureSubscriptionGranted(subscribeResult);

                    var publishStopwatch = Stopwatch.StartNew();

                    var message = _factory
                        .CreateApplicationMessageBuilder()
                        .WithTopic(commandTopic)
                        .WithPayload(commandPayload)
                        .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)request.CommandQos)
                        .WithRetainFlag(request.CommandRetain)
                        .Build();

                    // 顺序不能互换：先让回调开始接收，再发布命令。
                    Volatile.Write(ref acceptMessages, 1);

                    var publishResult = await client.PublishAsync(message, token);

                    publishStopwatch.Stop();

                    if (!publishResult.IsSuccess &&
                        publishResult.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers)
                    {
                        throw new AppException(
                            $"MQTT 发布失败：{publishResult.ReasonCode}",
                            502);
                    }

                    MqttReceivedMessage receivedMessage;

                    try
                    {
                        receivedMessage = await completion.Task.WaitAsync(
                            TimeSpan.FromMilliseconds(request.WaitTimeoutMilliseconds),
                            token);
                    }
                    catch (TimeoutException)
                    {
                        throw new AppException(
                            $"命令已发布，但 {request.WaitTimeoutMilliseconds} ms 内没有收到匹配的 command-result",
                            504);
                    }

                    totalStopwatch.Stop();

                    return new MqttPublishAndReceiveResult
                    {
                        Publish = new MqttPublishResult
                        {
                            Topic = commandTopic,
                            Qos = request.CommandQos,
                            Retain = request.CommandRetain,
                            ReasonCode = publishResult.ReasonCode.ToString(),
                            ElapsedMilliseconds = publishStopwatch.ElapsedMilliseconds
                        },
                        ReceivedMessage = receivedMessage,
                        TotalElapsedMilliseconds = totalStopwatch.ElapsedMilliseconds
                    };
                },
                cancellationToken,
                client =>
                {
                    // 必须在 Connect / Subscribe 之前注册，避免订阅成功后
                    // 回执到达时还没有消息处理器。
                    client.ApplicationMessageReceivedAsync += eventArgs =>
                    {
                        if (Volatile.Read(ref acceptMessages) == 0)
                        {
                            return Task.CompletedTask;
                        }

                        var message = eventArgs.ApplicationMessage;
                        var payload = message.ConvertPayloadToString() ?? string.Empty;

                        // 只接收包含 commandId 的回执，过滤其他命令的结果。
                        if (payloadMarker != null &&
                            payload.IndexOf(payloadMarker, StringComparison.Ordinal) < 0)
                        {
                            return Task.CompletedTask;
                        }

                        completion.TrySetResult(new MqttReceivedMessage
                        {
                            Topic = message.Topic,
                            Payload = payload,
                            Qos = (int)message.QualityOfServiceLevel,
                            Retain = message.Retain,
                            ReceivedAt = DateTime.UtcNow
                        });

                        return Task.CompletedTask;
                    };
                });
        }


        // ============================================================
        // 5. 公共执行模板：创建 → 配置 → 连接 → 执行 → 断开
        // ============================================================
        private async Task<T> ExecuteAsync<T>(
            MqttBrokerRequest request,
            Func<IMqttClient, MqttClientConnectResult, CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken,
            Action<IMqttClient>? configureClient = null)
        {
            using var client = _factory.CreateMqttClient();

            try
            {
                configureClient?.Invoke(client);

                var options = BuildOptions(request);

                var connectResult = await ConnectAsync(
                    client,
                    options,
                    request.ConnectTimeoutMilliseconds,
                    cancellationToken);

                return await operation(client, connectResult, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 外部没有取消，说明是我们自己设的连接超时。
                throw new AppException("MQTT 操作超时，请检查 Broker、网络和超时设置", 504);
            }
            catch (MqttCommunicationTimedOutException exception)
            {
                throw new AppException($"MQTT 通信超时：{exception.Message}", 504);
            }
            catch (MqttConnectingFailedException exception)
            {
                // 必须放在 MqttCommunicationException 之前。
                throw new AppException($"Broker 拒绝连接：{exception.Message}", 502);
            }
            catch (MqttCommunicationException exception)
            {
                // 覆盖 MqttClientDisconnectedException、MqttClientNotConnectedException 等。
                throw new AppException($"MQTT 通信失败：{exception.Message}", 502);
            }
            catch (MqttConfigurationException exception)
            {
                throw new AppException($"MQTT 配置错误：{exception.Message}", 400);
            }
            catch (MqttProtocolViolationException exception)
            {
                throw new AppException($"MQTT 协议错误：{exception.Message}", 502);
            }
            catch (TimeoutException)
            {
                throw new AppException("等待 Broker 响应超时", 504);
            }
            catch (ArgumentException exception)
            {
                throw new AppException($"MQTT 参数错误：{exception.Message}", 400);
            }
            finally
            {
                // 无论成功、失败、超时，都尽量正常断开。
                await DisconnectIfNeededAsync(client);
            }
        }


        // ============================================================
        // 6. 连接参数
        // ============================================================
        private MqttClientOptions BuildOptions(MqttBrokerRequest request)
        {
            var builder = _factory
                .CreateClientOptionsBuilder()
                .WithTcpServer(request.Host.Trim(), request.Port)
                .WithClientId(request.ClientId.Trim())
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithCleanStart(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
                .WithTimeout(TimeSpan.FromMilliseconds(request.ConnectTimeoutMilliseconds));

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                builder.WithCredentials(
                    request.Username.Trim(),
                    request.Password ?? string.Empty);
            }

            return builder.Build();
        }

        private static async Task<MqttClientConnectResult> ConnectAsync(
            IMqttClient client,
            MqttClientOptions options,
            int connectTimeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            using var connectTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            connectTimeout.CancelAfter(connectTimeoutMilliseconds);

            var result = await client.ConnectAsync(options, connectTimeout.Token);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new AppException(
                    $"Broker 拒绝连接：{result.ResultCode}",
                    502);
            }

            return result;
        }

        private async Task DisconnectIfNeededAsync(IMqttClient client)
        {
            if (!client.IsConnected)
            {
                return;
            }

            try
            {
                var options = _factory
                    .CreateClientDisconnectOptionsBuilder()
                    .Build();

                await client.DisconnectAsync(options, CancellationToken.None);
            }
            catch
            {
                // 清理阶段不能用一个新异常覆盖主流程异常。
            }
        }


        // ============================================================
        // 7. 校验
        // ============================================================
        private void ValidateBroker(MqttBrokerRequest request)
        {
            var host = request.Host?.Trim();

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new AppException("host 不能为空", 400);
            }

            if (host.Contains("://", StringComparison.Ordinal))
            {
                throw new AppException("host 不能包含 mqtt:// 等协议前缀", 400);
            }

            if (!_allowedHosts.Contains(host))
            {
                throw new AppException($"主机 {host} 不在 MQTT 诊断白名单中", 403);
            }

            if (request.Port is < 1 or > 65535)
            {
                throw new AppException("port 必须在 1 到 65535 之间", 400);
            }

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                throw new AppException("clientId 不能为空", 400);
            }

            if (request.ConnectTimeoutMilliseconds is < 500 or > 120000)
            {
                throw new AppException("connectTimeoutMilliseconds 必须在 500 到 120000 之间", 400);
            }
        }

        private static void ValidatePublish(MqttPublishRequest request)
        {
            var topic = request.Topic?.Trim();

            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new AppException("topic 不能为空", 400);
            }

            if (topic.Contains('+') || topic.Contains('#'))
            {
                throw new AppException("发布 Topic 不能包含 + 或 # 通配符", 400);
            }

            ValidateQos(request.Qos);
        }

        private static void ValidateReceive(MqttReceiveOnceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TopicFilter))
            {
                throw new AppException("topicFilter 不能为空", 400);
            }

            ValidateQos(request.Qos);

            if (request.WaitTimeoutMilliseconds is < 500 or > 60000)
            {
                throw new AppException("waitTimeoutMilliseconds 必须在 500 到 60000 之间", 400);
            }
        }

        private static void ValidatePublishAndReceive(MqttPublishAndReceiveRequest request)
        {
            var commandTopic = request.CommandTopic?.Trim();

            if (string.IsNullOrWhiteSpace(commandTopic))
            {
                throw new AppException("commandTopic 不能为空", 400);
            }

            if (commandTopic.Contains('+') || commandTopic.Contains('#'))
            {
                throw new AppException("commandTopic 不能包含 + 或 # 通配符", 400);
            }

            ValidateQos(request.CommandQos);

            var resultTopic = request.ResultTopic?.Trim();

            if (string.IsNullOrWhiteSpace(resultTopic))
            {
                throw new AppException("resultTopic 不能为空", 400);
            }

            if (resultTopic.Contains('+') || resultTopic.Contains('#'))
            {
                throw new AppException("resultTopic 不能包含 + 或 # 通配符", 400);
            }

            ValidateQos(request.ResultQos);

            if (request.WaitTimeoutMilliseconds is < 500 or > 60000)
            {
                throw new AppException("waitTimeoutMilliseconds 必须在 500 到 60000 之间", 400);
            }
        }

        private static void ValidateQos(int qos)
        {
            if (qos is < 0 or > 2)
            {
                throw new AppException("qos 只能是 0、1 或 2", 400);
            }
        }

        private static void EnsureSubscriptionGranted(MqttClientSubscribeResult result)
        {
            foreach (var item in result.Items)
            {
                if (item.ResultCode is MqttClientSubscribeResultCode.GrantedQoS0
                    or MqttClientSubscribeResultCode.GrantedQoS1
                    or MqttClientSubscribeResultCode.GrantedQoS2)
                {
                    continue;
                }

                var statusCode = item.ResultCode switch
                {
                    MqttClientSubscribeResultCode.TopicFilterInvalid => 400,
                    MqttClientSubscribeResultCode.NotAuthorized => 403,
                    _ => 502
                };

                throw new AppException($"订阅被拒绝：{item.ResultCode}", statusCode);
            }
        }
    }
}
