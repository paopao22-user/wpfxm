using Day44.MafWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册 Controller 所需的 MVC 服务和 JSON 模型绑定能力。
builder.Services.AddControllers();

// DeepSeekChatService 只持有可复用的 ChatClient；
// 每次请求的 Agent 和 Session 都在 AskAsync 内单独创建。
builder.Services.AddSingleton<DeepSeekChatService>();

var app = builder.Build();

// 启动时主动解析一次：配置缺失就立即报错，而不是等第一个请求。
_ = app.Services.GetRequiredService<DeepSeekChatService>();

app.MapControllers();
app.Run();
