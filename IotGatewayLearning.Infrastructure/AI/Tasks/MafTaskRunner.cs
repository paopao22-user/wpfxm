using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.AI.Tasks;
using IotGatewayLearning.Infrastructure.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace IotGatewayLearning.Infrastructure.AI.Tasks
{
    // 只给 DeepSeek 聊天请求补 thinking.type=disabled，保留 SDK 生成的其他字段。
    public sealed class DeepSeekToolModeHandler : DelegatingHandler
    {
        public DeepSeekToolModeHandler() : base(new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct)
        {
            if (request.Method == HttpMethod.Post &&
                request.RequestUri?.Host == "api.deepseek.com" &&
                request.RequestUri.AbsolutePath.EndsWith("/chat/completions") &&
                request.Content is not null)
            {
                var old = request.Content;
                var node = JsonNode.Parse(await old.ReadAsStringAsync(ct))!.AsObject();
                node["thinking"] = new JsonObject { ["type"] = "disabled" };

                request.Content = new StringContent(
                    node.ToJsonString(),
                    Encoding.UTF8,
                    "application/json");
                old.Dispose();
            }

            return await base.SendAsync(request, ct);
        }
    }

    public sealed class MafTaskRunner : IAiTaskRunner
    {
        private readonly AiModelOptions _settings;

        public MafTaskRunner(IOptions<AiModelOptions> options)
        {
            _settings = options.Value;
        }

        public async Task<string> RunAsync(
            string question,
            DeviceQueryTools tools,
            CancellationToken ct)
        {
            using var http = new HttpClient(new DeepSeekToolModeHandler())              // ① 自建 HttpClient
            {
                Timeout = TimeSpan.FromSeconds(130)
            };

            var client = new OpenAIClient(
                new ApiKeyCredential(_settings.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri("https://api.deepseek.com"),         
                    Transport = new HttpClientPipelineTransport(http),      
                    // 一次按钮点击不应隐含多次付费调用。
                    RetryPolicy = new ClientRetryPolicy(0),                             // ③ 关掉自动重试
                    NetworkTimeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
                });

            AIAgent agent = client.GetChatClient(_settings.ModelId).AsAIAgent(          // ④ 组装 Agent
                new ChatClientAgentOptions
                {
                    Name = "DeviceInvestigator",
                    ChatOptions = new ChatOptions
                    {
                        Instructions = "只根据真实工具返回回答，事实与假设分开。先查相关数据；失败或截断必须说明。禁止控制设备。最多4次工具调用。",
                        Tools = new List<AITool>
                        {
                            AIFunctionFactory.Create(
                                (Func<Task<string>>)tools.GetDevice,
                                name: "GetDevice",
                                description: "读取本任务绑定设备的资料，不改变设备状态"),
                            AIFunctionFactory.Create(
                                (Func<Task<string>>)tools.GetHistory,
                                name: "GetHistory",
                                description: "读取本任务绑定点位最近一分钟样本，包括质量和是否截断"),
                            AIFunctionFactory.Create(
                                (Func<Task<string>>)tools.GetAlarms,
                                name: "GetAlarms",
                                description: "读取本任务绑定设备最多20条活动或已确认告警")
                        }
                    }
                });

            var session = await agent.CreateSessionAsync(ct);               // ⑤ 建会话 + 跑
            var response = await agent.RunAsync(question, session, cancellationToken: ct);
            var text = response.Text;

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("模型没有最终回答");

            return text;
        }
    }
}
