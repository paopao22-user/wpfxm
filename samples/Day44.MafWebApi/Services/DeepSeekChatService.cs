using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Day44.MafWebApi.Models;

namespace Day44.MafWebApi.Services;

public sealed class DeepSeekChatService
{
    private readonly ChatClient chat;
    private readonly string modelId;

    // IConfiguration 由 ASP.NET Core 提供，读取 appsettings 和 User Secrets。
    public DeepSeekChatService(IConfiguration configuration)
    {
        var key = configuration["DeepSeek:ApiKey"];
        var endpoint = configuration["DeepSeek:Endpoint"] ?? "";
        modelId = configuration["DeepSeek:ModelId"] ?? "";

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("请配置DeepSeek:ApiKey");

        if (endpoint != "https://api.deepseek.com" || string.IsNullOrWhiteSpace(modelId))
            throw new InvalidOperationException("请核对DeepSeek地址和模型名");

        // 客户端可以复用；Agent 和 Session 在方法内按请求单独创建。
        var client = new OpenAIClient(new ApiKeyCredential(key),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint),
                RetryPolicy = new ClientRetryPolicy(0),
                NetworkTimeout = TimeSpan.FromSeconds(120)
            });

        chat = client.GetChatClient(modelId);
    }

    // question 来自请求 DTO；requestAborted 来自 HTTP 请求的取消信号。
    public async Task<ChatReply> AskAsync(string question, CancellationToken requestAborted)
    {
        var text = question?.Trim() ?? "";
        if (text.Length is < 1 or > 1000)
            throw new ArgumentException("问题长度必须为1至1000字符");

        // 把客户端取消和服务端 120 秒限制组合在一起。
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        timeout.CancelAfter(TimeSpan.FromSeconds(120));

        AIAgent agent = chat.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "BeginnerAssistant",
            ChatOptions = new ChatOptions
            {
                Instructions = "你是中文教学助手。解释概念，不宣称查询或控制了真实设备。",
                MaxOutputTokens = 8192
            }
        });

        var session = await agent.CreateSessionAsync(timeout.Token);
        var response = await agent.RunAsync(text, session, cancellationToken: timeout.Token);

        if (string.IsNullOrWhiteSpace(response.Text))
            throw new InvalidOperationException("模型未返回最终文本");

        return new ChatReply(
            modelId,
            response.Text,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount);
    }
}
