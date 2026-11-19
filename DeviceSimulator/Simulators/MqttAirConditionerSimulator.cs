using System.Text.Json;
using System.Text.Json.Nodes;
using IotGatewayLearning.DeviceSimulator.Models.Command;
using IotGatewayLearning.DeviceSimulator.Models.CommandResult;
using IotGatewayLearning.DeviceSimulator.Models.Common;
using IotGatewayLearning.DeviceSimulator.Models.Telemetry;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace IotGatewayLearning.DeviceSimulator.Simulators
{
    /// <summary>
    /// M2：MQTT 空调模拟器。
    /// 连接 EMQX、每 2 秒发布遥测、订阅命令、执行命令并发布 command-result。
    /// Will、在线/离线状态留到 M3；幂等、过期校验留到 M4。
    /// </summary>
    public static class MqttAirConditionerSimulator
    {
        private const string DeviceCode = "AC-001";
        private const string BrokerHost = "127.0.0.1";
        private const int BrokerPort = 1883;
        private const string SchemaVersion = "1.0";

        private static readonly string TelemetryTopic =
            $"iot/devices/{DeviceCode}/telemetry";

        private static readonly string CommandTopic =
            $"iot/devices/{DeviceCode}/command";

        private static readonly string CommandResultTopic =
            $"iot/devices/{DeviceCode}/command-result";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private static readonly HashSet<string> AllowedModes = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "cool",
            "heat",
            "fan",
            "auto"
        };

        // M2 开始，遥测线程和命令回调线程都会读写这些状态，必须加锁。
        private static readonly object StateLock = new();

        private static bool _power = true;
        private static string _mode = "cool";
        private static double _targetTemperature = 25.0;
        private static double _indoorTemperature = 28.0;
        private static long _sequence;

        public static async Task RunAsync()
        {
            // Ctrl+C 时取消定时器和连接等待，走 finally 正常断开。
            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            // MQTTnet 5 使用 MqttClientFactory，不是旧教程里的 MqttFactory。
            var factory = new MqttClientFactory();
            using var client = factory.CreateMqttClient();

            // 只生成连接参数，此时还没有发起网络连接。
            var options = factory
                .CreateClientOptionsBuilder()
                .WithTcpServer(BrokerHost, BrokerPort)
                .WithClientId($"simulator-{DeviceCode}")
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithCleanStart(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
                .WithTimeout(TimeSpan.FromSeconds(5))
                .Build();

            // 先注册接收回调，再订阅；避免订阅成功后第一条命令到达时还没有处理器。
            client.ApplicationMessageReceivedAsync += eventArgs =>
                HandleCommandAsync(factory, client, eventArgs, shutdown.Token);

            try
            {
                // 建立 TCP 连接，并完成 MQTT CONNECT / CONNACK。
                var connectResult = await client.ConnectAsync(
                    options,
                    shutdown.Token);

                Console.WriteLine($"MQTT 空调已连接：{connectResult.ResultCode}");
                Console.WriteLine($"Broker：{BrokerHost}:{BrokerPort}");
                Console.WriteLine($"ClientId：simulator-{DeviceCode}");
                Console.WriteLine($"遥测 Topic：{TelemetryTopic}");
                Console.WriteLine($"命令 Topic：{CommandTopic}");

                var subscribeOptions = factory
                    .CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(filter => filter
                        .WithTopic(CommandTopic)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                    .Build();

                var subscribeResult = await client.SubscribeAsync(
                    subscribeOptions,
                    shutdown.Token);

                Console.WriteLine(
                    $"订阅结果：{string.Join(", ", subscribeResult.Items.Select(item => item.ResultCode))}");
                Console.WriteLine("每 2 秒发布一条遥测，按 Ctrl+C 停止。");
                Console.WriteLine();

                // PeriodicTimer 比 while + Task.Delay 更稳，不会因为发布耗时导致周期漂移。
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

                while (await timer.WaitForNextTickAsync(shutdown.Token))
                {
                    var telemetry = CreateTelemetryMessage();

                    // M1 只发遥测，所以 Retain 固定为 false。
                    // 遥测是流，不是状态；Retain 会把最后一条旧数据留给新订阅者。
                    await PublishJsonAsync(
                        factory,
                        client,
                        TelemetryTopic,
                        telemetry,
                        retain: false,
                        shutdown.Token);

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] " +
                        $"室内={telemetry.Data.IndoorTemperature:F1}℃，" +
                        $"室外={telemetry.Data.OutdoorTemperature:F1}℃，" +
                        $"设定={telemetry.Data.TargetTemperature:F1}℃，" +
                        $"模式={telemetry.Data.Mode}，" +
                        $"序号={telemetry.Data.Sequence}");
                }
            }
            catch (OperationCanceledException)
            {
                // Ctrl+C 取消 PeriodicTimer，属于正常停止。
            }
            catch (Exception exception)
            {
                // Broker 未启动、端口错误、网络中断都会进入这里。
                Console.WriteLine($"MQTT 运行失败：{exception.Message}");
            }
            finally
            {
                if (client.IsConnected)
                {
                    try
                    {
                        var disconnectOptions = factory
                            .CreateClientDisconnectOptionsBuilder()
                            .Build();

                        await client.DisconnectAsync(
                            disconnectOptions,
                            CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"断开连接失败：{exception.Message}");
                    }
                }

                Console.WriteLine("MQTT 空调已停止。");
            }
        }

        private static TelemetryMessage CreateTelemetryMessage()
        {
            double indoorTemperature;
            double outdoorTemperature;
            double targetTemperature;
            bool power;
            string mode;
            long sequence;

            // 只在读写共享状态时加锁，序列化和发布放到锁外，避免阻塞命令回调。
            lock (StateLock)
            {
                // 室外温度在 31.0 到 33.0 之间缓慢随机变化。
                outdoorTemperature = 31.0 + Random.Shared.NextDouble() * 2.0;

                if (_power && _mode == "cool")
                {
                    // 制冷时室温逐步靠近设定值，不会瞬间跳到目标温度。
                    _indoorTemperature +=
                        (_targetTemperature - _indoorTemperature) * 0.15;
                }
                else
                {
                    // 关机或非制冷时，室温缓慢靠近室外温度。
                    _indoorTemperature +=
                        (outdoorTemperature - _indoorTemperature) * 0.03;
                }

                // 加入很小的传感器波动。
                _indoorTemperature +=
                    (Random.Shared.NextDouble() - 0.5) * 0.2;

                indoorTemperature = Math.Round(_indoorTemperature, 1);
                targetTemperature = Math.Round(_targetTemperature, 1);
                power = _power;
                mode = _mode;
                sequence = ++_sequence;
            }

            return new TelemetryMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                DeviceCode = DeviceCode,
                MessageType = MqttMessageTypes.Telemetry,
                Timestamp = DateTimeOffset.UtcNow,
                SchemaVersion = SchemaVersion,
                Data = new TelemetryData
                {
                    IndoorTemperature = indoorTemperature,
                    OutdoorTemperature = Math.Round(outdoorTemperature, 1),
                    TargetTemperature = targetTemperature,
                    Power = power,
                    Mode = mode,
                    Sequence = sequence
                }
            };
        }

        private static async Task PublishJsonAsync<T>(
            MqttClientFactory factory,
            IMqttClient client,
            string topic,
            T value,
            bool retain,
            CancellationToken cancellationToken)
        {
            // 把消息对象序列化成 camelCase JSON。
            var payload = JsonSerializer.Serialize(value, JsonOptions);

            // 一条 MQTT 应用消息至少包含 Topic、Payload、QoS 和 Retain。
            var message = factory
                .CreateApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(
                    MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(retain)
                .Build();

            // QoS 1：等待 Broker 返回 PUBACK。
            await client.PublishAsync(message, cancellationToken);
        }

        /// <summary>
        /// M2：收到命令消息后的处理入口。
        /// 负责反序列化、调用执行器、发布 command-result。
        /// </summary>
        private static async Task HandleCommandAsync(
            MqttClientFactory factory,
            IMqttClient client,
            MqttApplicationMessageReceivedEventArgs eventArgs,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(
                    eventArgs.ApplicationMessage.Topic,
                    CommandTopic,
                    StringComparison.Ordinal))
            {
                return;
            }

            var payload = eventArgs.ApplicationMessage.ConvertPayloadToString();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到命令：{payload}");

            CommandMessage? command;
            try
            {
                command = JsonSerializer.Deserialize<CommandMessage>(
                    payload,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                // M2 暂时只记录；解析失败时拿不到 messageId，M4 再补失败回执。
                Console.WriteLine($"命令 JSON 解析失败：{exception.Message}");
                return;
            }

            if (command is null)
            {
                Console.WriteLine("命令为空，已忽略。");
                return;
            }

            CommandResultData result;
            try
            {
                result = ExecuteCommand(command);
            }
            catch (Exception exception)
            {
                result = Failure(
                    command.MessageId ?? "unknown",
                    CommandResultCodes.InternalError,
                    $"执行命令时发生异常：{exception.Message}");
            }

            try
            {
                await PublishJsonAsync(
                    factory,
                    client,
                    CommandResultTopic,
                    new CommandResultMessage
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                        DeviceCode = DeviceCode,
                        MessageType = MqttMessageTypes.CommandResult,
                        Timestamp = DateTimeOffset.UtcNow,
                        SchemaVersion = SchemaVersion,
                        Data = result
                    },
                    retain: false,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 程序正在退出，不再补发回执。
                return;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"命令回执发布失败：{exception.Message}");
                return;
            }

            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] 命令回执：" +
                $"commandId={result.CommandId}，" +
                $"success={result.Success}，" +
                $"code={result.Code}，" +
                $"message={result.Message}");
        }

        /// <summary>
        /// M2：执行空调命令。
        /// 只处理 powerOn、powerOff、setTemperature、setMode 四种动作。
        /// </summary>
        private static CommandResultData ExecuteCommand(CommandMessage command)
        {
            var commandId = command.MessageId ?? "unknown";
            var completedAt = DateTimeOffset.UtcNow;

            if (command.Data is null)
            {
                return Failure(
                    commandId,
                    CommandResultCodes.InvalidCommand,
                    "命令 data 不能为空。");
            }

            if (!string.Equals(
                    command.DeviceCode,
                    DeviceCode,
                    StringComparison.Ordinal))
            {
                return Failure(
                    commandId,
                    CommandResultCodes.DeviceMismatch,
                    $"设备编号不匹配：{command.DeviceCode}");
            }

            if (!string.Equals(
                    command.MessageType,
                    MqttMessageTypes.Command,
                    StringComparison.Ordinal))
            {
                return Failure(
                    commandId,
                    CommandResultCodes.InvalidMessageType,
                    $"消息类型不是 command：{command.MessageType}");
            }

            var action = command.Data.Action?.Trim();
            if (string.IsNullOrWhiteSpace(action))
            {
                return Failure(
                    commandId,
                    CommandResultCodes.InvalidCommand,
                    "action 不能为空。");
            }

            // 命令回调和遥测定时器可能同时访问这些字段，修改时必须加锁。
            lock (StateLock)
            {
                switch (action.ToLowerInvariant())
                {
                    case "poweron":
                        _power = true;
                        return Success(
                            commandId,
                            "空调已开机。",
                            completedAt);

                    case "poweroff":
                        _power = false;
                        return Success(
                            commandId,
                            "空调已关机。",
                            completedAt);

                    case "settemperature":
                        if (!TryGetDoubleParameter(
                                command.Data.Parameters,
                                "targetTemperature",
                                out var targetTemperature))
                        {
                            return Failure(
                                commandId,
                                CommandResultCodes.InvalidParameter,
                                "setTemperature 需要 number 类型的 targetTemperature。");
                        }

                        if (targetTemperature < 16.0 || targetTemperature > 30.0)
                        {
                            return Failure(
                                commandId,
                                CommandResultCodes.OutOfRange,
                                "targetTemperature 必须在 16.0 到 30.0 之间。");
                        }

                        _targetTemperature = Math.Round(targetTemperature, 1);
                        return Success(
                            commandId,
                            $"设定温度已修改为 {_targetTemperature:F1}℃。",
                            completedAt);

                    case "setmode":
                        if (!TryGetStringParameter(
                                command.Data.Parameters,
                                "mode",
                                out var mode) ||
                            string.IsNullOrWhiteSpace(mode))
                        {
                            return Failure(
                                commandId,
                                CommandResultCodes.InvalidParameter,
                                "setMode 需要 string 类型的 mode。");
                        }

                        var normalizedMode = mode.Trim().ToLowerInvariant();
                        if (!AllowedModes.Contains(normalizedMode))
                        {
                            return Failure(
                                commandId,
                                CommandResultCodes.InvalidParameter,
                                "mode 只允许 cool、heat、fan 或 auto。");
                        }

                        _mode = normalizedMode;
                        return Success(
                            commandId,
                            $"运行模式已修改为 {_mode}。",
                            completedAt);

                    default:
                        return Failure(
                            commandId,
                            CommandResultCodes.UnsupportedAction,
                            $"不支持的命令动作：{action}");
                }
            }
        }

        private static bool TryGetDoubleParameter(
            JsonObject? parameters,
            string name,
            out double value)
        {
            value = 0;

            if (parameters is null ||
                !parameters.TryGetPropertyValue(name, out var node) ||
                node is not JsonValue jsonValue)
            {
                return false;
            }

            return jsonValue.TryGetValue(out value);
        }

        private static bool TryGetStringParameter(
            JsonObject? parameters,
            string name,
            out string? value)
        {
            value = null;

            if (parameters is null ||
                !parameters.TryGetPropertyValue(name, out var node) ||
                node is not JsonValue jsonValue)
            {
                return false;
            }

            return jsonValue.TryGetValue(out value);
        }

        private static CommandResultData Success(
            string commandId,
            string message,
            DateTimeOffset completedAt)
        {
            return new CommandResultData
            {
                CommandId = commandId,
                Success = true,
                Code = CommandResultCodes.Ok,
                Message = message,
                CompletedAt = completedAt
            };
        }

        private static CommandResultData Failure(
            string commandId,
            string code,
            string message)
        {
            return new CommandResultData
            {
                CommandId = commandId,
                Success = false,
                Code = code,
                Message = message,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
