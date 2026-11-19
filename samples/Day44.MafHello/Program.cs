using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

// ① 控制台从环境变量读密钥；WebAPI 会从配置系统读。
var key = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");       // 它从当前进程的环境变量里读 DEEPSEEK_API_KEY
if (string.IsNullOrWhiteSpace(key))
    throw new InvalidOperationException("请先配置DEEPSEEK_API_KEY");

// ② 创建客户端不等于发出请求，构造时不会访问 DeepSeek。
var client = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.deepseek.com"),     // Endpoint 填基础地址
    RetryPolicy = new ClientRetryPolicy(0),             // 不自动重试，避免重复计费
    NetworkTimeout = TimeSpan.FromSeconds(120)          // 这是 SDK 层的 HTTP 网络超时。
});

// ③ 选择模型；这个标识必须由 DeepSeek 服务接受。
var chat = client.GetChatClient("deepseek-flash");

// ④ 把模型客户端适配成 MAF Agent。
//    Name 是应用内名称；Instructions 是发给模型的任务约束。
AIAgent agent = chat.AsAIAgent(new ChatClientAgentOptions       // AsAIAgent 是一个适配方法：把底层的聊天客户端包装成 MAF 的 AIAgent（智能体）
{
    Name = "FirstDeviceAssistant",                      // 这是应用内名称，方便日志、调试和多 Agent 区分。
    ChatOptions = new ChatOptions
    {
        Instructions = "用中文回答。只解释输入事实，不调用工具，不控制设备。",        // 这是发给模型的任务约束，告诉它要做什么。
        MaxOutputTokens = 8192                          // 这是输出预算，限制模型这次最多生成多少 Token。
    }
});

// ⑤ 创建本次对话的 Session，并设置 120 秒应用级超时。
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
var session = await agent.CreateSessionAsync(timeout.Token);

// ⑥ RunAsync 才进入真正的模型调用阶段。
var response = await agent.RunAsync(
    "三个有效温度为80、82、88摄氏度，未提供阈值。请说明观察结果。",
    session,
    cancellationToken: timeout.Token);

// ⑦ Text 是最终文本；Usage 缺失时是 null，不要当作 0。
Console.WriteLine(response.Text);
Console.WriteLine($"输入Token：{response.Usage?.InputTokenCount?.ToString() ?? "未提供"}");
Console.WriteLine($"输出Token：{response.Usage?.OutputTokenCount?.ToString() ?? "未提供"}");