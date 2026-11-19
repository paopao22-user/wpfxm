using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace IotGatewayLearning.Infrastructure.AI
{
    // 模型访问适配器：只负责“把两段文字发给 DeepSeek 并取回最终文本”，
    // 不查数据库、不创建分析记录、不生成告警。
    public sealed class DeepSeekMafClient : IAiTextClient
    {
        private readonly ChatClient _chat;              // DeepSeek 的聊天客户端，负责与模型进行交互
        private readonly AiModelOptions _settings;      // 模型配置：模型名、超时、Token 预算等

        // options 由依赖注入提供，构造阶段只准备客户端，不发送付费请求。
        public DeepSeekMafClient(IOptions<AiModelOptions> options)
        {
            _settings = options.Value;      // 读取模型配置：模型名、超时、Token 预算等

            var client = new OpenAIClient(
                new ApiKeyCredential(_settings.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(_settings.Endpoint),     // DeepSeek 服务端地址，必须以 https:// 开头
                    // 教学阶段不自动重试：一次按钮点击不应隐含多次付费调用。
                    RetryPolicy = new ClientRetryPolicy(0),     // 禁止 SDK 自动重试，避免重复计费
                    NetworkTimeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds) // 网络请求超时，超过该时间会触发 OperationCanceledException
                });

            _chat = client.GetChatClient(_settings.ModelId);    // 获取指定模型的聊天客户端，用于发送请求和接收响应
        }

        // 服务端配置的模型标识；失败记录也要能写入 model_id。
        public string ModelId => _settings.ModelId;

        // system 约束任务，input 承载事实；返回最终文字及可选用量。
        public async Task<AiTextResult> CompleteAsync(
            string system,
            string input,
            CancellationToken ct)
        {
            // 每次调用创建独立 Agent 与 Session，不共享不同设备/不同请求的对话历史。
            AIAgent agent = _chat.AsAIAgent(new ChatClientAgentOptions
            {
                Name = "DeviceAnalysis",
                ChatOptions = new ChatOptions
                {
                    Instructions = system,                      // 系统指令，告诉模型如何处理输入数据
                    MaxOutputTokens = _settings.MaxOutputTokens // 限制模型输出的最大 Token 数量，避免超出保存限制
                }
            });

            try
            {
                var session = await agent.CreateSessionAsync(ct);       // 创建一个新的会话，用于与模型进行交互
                var response = await agent.RunAsync(input, session, cancellationToken: ct); // 发送输入文本给模型，并等待模型的响应

                if (string.IsNullOrWhiteSpace(response.Text))
                    throw new AiProviderException("EmptyOutput", "模型未返回可显示的文本");

                if (response.Text.Length > 16000)
                    throw new AiProviderException("OutputTooLong", "模型输出超出本章保存限制");

                // Token 用量可缺失；null 表示厂商未提供，不能伪造为 0。
                return new AiTextResult(
                    response.Text,
                    response.Usage?.InputTokenCount,
                    response.Usage?.OutputTokenCount);
            }
            catch (ClientResultException ex)
            {
                throw ex.Status switch
                {
                    401 or 403 => new AiProviderException("ProviderAuth", "模型服务拒绝访问，请检查服务端凭据和权限"),
                    429 => new AiProviderException("ProviderLimit", "模型服务限流或额度不足，请检查服务端账户"),
                    400 or 404 => new AiProviderException("ProviderRequest", "模型名、地址或请求参数不被服务支持"),
                    _ => new AiProviderException("ProviderUnavailable", "模型服务暂时不可用")
                };
            }
            catch (HttpRequestException)
            {
                throw new AiProviderException("ProviderNetwork", "无法连接模型服务");
            }
        }
    }
}
