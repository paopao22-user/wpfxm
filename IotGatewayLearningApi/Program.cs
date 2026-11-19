using IotGatewayLearning.Application.AI;
using IotGatewayLearning.Application.AI.Advisories;
using IotGatewayLearning.Application.AI.Inspections;
using IotGatewayLearning.Application.AI.Knowledge;
using IotGatewayLearning.Application.AI.Mcp;
using IotGatewayLearning.Application.AI.Tasks;
using IotGatewayLearning.Application.DTOs.Modbus;
using IotGatewayLearning.Application.DTOs.Mqtt;
using IotGatewayLearning.Application.DTOs.Serial;
using IotGatewayLearning.Application.Services;
using IotGatewayLearning.Application.Services.Implementations;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearning.DeviceGateway.Modbus;
using IotGatewayLearning.Domain.AI;
using IotGatewayLearning.Domain.AI.Advisories;
using IotGatewayLearning.Domain.AI.Knowledge;
using IotGatewayLearning.Domain.AI.Tasks;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.DeviceGateway.Mqtt;
using IotGatewayLearning.DeviceGateway.Serial;
using IotGatewayLearning.Infrastructure.AI;
using IotGatewayLearning.Infrastructure.AI.Advisories;
using IotGatewayLearning.Infrastructure.AI.Inspections;
using IotGatewayLearning.Infrastructure.AI.Knowledge;
using IotGatewayLearning.Infrastructure.AI.Mcp;
using IotGatewayLearning.Infrastructure.AI.Tasks;
using IotGatewayLearning.Infrastructure.Data;
using IotGatewayLearning.Infrastructure.Repositories;
using IotGatewayLearning.Infrastructure.Repositories.Implementations;
using IotGatewayLearning.Infrastructure.Repositories.Interfaces;
using IotGatewayLearning.Infrastructure.Storage;
using IotGatewayLearningApi.AI.Advisories;
using IotGatewayLearningApi.Middleware;
using IotGatewayLearningApi.Security;
using IotGatewayLearningApi.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;   // FormOptions（上传缓冲设置）
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;
using Serilog;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);   //创建整个 ASP.NET Core 应用的“装配器”

// ==============================
// JWT Authentication jwt认证
// ==============================
//builder.Services是ASP.NET Core 的 DI 服务容器。
//builder.Services.AddAuthentication(...)：给整个 WebAPI 注册一套身份认证服务
//JwtBearerDefaults.AuthenticationScheme:  当前系统默认使用 Bearer Token 作为身份认证方式
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //配置怎么验证jwt
    .AddJwtBearer(options =>        
    {
        options.TokenValidationParameters = new TokenValidationParameters   //TokenValidationParameters: Token 的验证参数
        {
            // ==============================
            // 1. 验证签发者
            // ==============================
            ValidateIssuer = true,  //  验证签发者

            ValidIssuer = builder.Configuration["Jwt:Issuer"],  //从配置里面读取

            // ==============================
            // 2. 验证接收者
            // ==============================
            ValidateAudience = true,    //验证接收者

            ValidAudience = builder.Configuration["Jwt:Audience"],

            // ==============================
            // 3. 验证过期时间
            // ==============================
            ValidateLifetime = true,

            // ==============================
            // 4. 验证签名
            // ==============================

            ValidateIssuerSigningKey = true,    //我要检查 JWT 的数字签名

            IssuerSigningKey =
                    new SymmetricSecurityKey(                   //new SymmetricSecurityKey(byte[]):把这一串字节包装成 JWT 可以使用的对称密钥对象,
                                                                //对称密钥为验证和授权用同一个密钥                                
                        Encoding.UTF8.GetBytes(                 // string类型 转为字节类型
                            builder.Configuration["Jwt:Key"]!   //读取配置里面的key密钥。 !是为了告诉编译器，我知道这里不会是 null，你别警告我
                        )
                    ),


            // ==============================
            // 5. 当前用户名 / Role 对应哪个 Claim
            // ==============================

            NameClaimType = ClaimTypes.Name,

            RoleClaimType = ClaimTypes.Role,


            // ==============================
            // 6. 不给过期 Token 额外宽限时间
            // ==============================

            ClockSkew = TimeSpan.Zero
        };


        // ==============================
        // JWT 401 / 403 统一响应:把 JWT 默认产生的 401、403 响应，改造成你项目统一的 ApiResponse JSON 格式。
        // ==============================
        //options.Events = new JwtBearerEvents{...} : JwtBearer，当某些 JWT 事件发生时，我要自己处理
        options.EventsType = typeof(RbacJwtEvents);
        

    });

// ==============================
// JWT Authorization 注册jwt授权服务
// ==============================
builder.Services.AddAuthorization(options =>
{
    //第一层 规则名：注册策略名字:我在授权系统里登记一条规则，它的名字叫 device:read.
    options.AddPolicy(PermissionCodes.DeviceRead,
    //第二层 规则内容：规定通过条件:使用这条规则的用户，必须有 `permission = device:read` 这条 Claim.
    policy => policy.RequireClaim("permission", PermissionCodes.DeviceRead));


    //规则名：在授权系统登记一条规则
    options.AddPolicy(PermissionCodes.DeviceControl,
    //规则内容： 规定通过条件
    policy => policy.RequireClaim("permission", PermissionCodes.DeviceControl));

    //管理用户接口要求当前请求的操作者有 `permission = user:manage`
    options.AddPolicy(PermissionCodes.UserManage,
        policy => policy.RequireClaim("permission", PermissionCodes.UserManage));

    //添加设备配置注册授权规则
    options.AddPolicy(PermissionCodes.DeviceConfigure,
        policy => policy.RequireClaim("permission", PermissionCodes.DeviceConfigure));

    //设备事件接入：只有带 ai:event:ingest 的账号/桥接程序才能提交巡检事件
    options.AddPolicy(PermissionCodes.AiEventIngest,
        policy => policy.RequireClaim("permission", PermissionCodes.AiEventIngest));

    //控制申请审批（Day50）：只有带 device:approve 的账号才能进审批队列/批准/驳回
    options.AddPolicy(PermissionCodes.DeviceApprove,
        policy => policy.RequireClaim("permission", PermissionCodes.DeviceApprove));

    //查看控制申请：device:control 或 device:approve 任一即可
    options.AddPolicy(PermissionCodes.ControlView,
        policy => policy.RequireAssertion(c =>
            c.User.HasClaim("permission", PermissionCodes.DeviceControl) ||
            c.User.HasClaim("permission", PermissionCodes.DeviceApprove)));

});



// ==============================
// Serilog 接管 ASP.NET Core 日志
// ==============================
// builder.Host.UseSerilog():Serilog 正式接管 ASP.NET Core 日志系统
builder.Host.UseSerilog(
    (context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(        // .ReadFrom.Configuration(context.Configuration): 去读 `appsettings.json` 里面的 `"Serilog"` 配置
                context.Configuration)
            .ReadFrom.Services(services)    //.ReadFrom.Services(services): Serilog 可以利用 DI 容器里已有的服务
            .Enrich.FromLogContext();       //.Enrich.FromLogContext(): 启用 LogContext 里的附加属性
    });

// ==============================
// 1.读取数据库连接字符串
// ==============================
// Add services to the container.
//连接字符串
var connectionString = builder.Configuration.GetConnectionString("Default") ?? throw new Exception("未配置数据库连接字符串 ConnectionStrings:Default");

// ==============================
// 2.注册 EF Core + MySQL
// ==============================

//注册AppDbContext
builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)); //ServerVersion.AutoDetect():它会根据连接信息判断 MySQL版本
    });

// ==============================
// 3.注册业务依赖
// ==============================

//注册UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//注册RbacJwtEvents
builder.Services.AddScoped<RbacJwtEvents>();

//注册 AuthService 登录业务
builder.Services.AddScoped<IAuthService, AuthService>();



//注册用户服务UserService
builder.Services.AddScoped<IUserService, UserService>();

//注册角色服务RoleService
builder.Services.AddScoped<IRoleService, RoleService>();

//注册权限服务PermissionService
builder.Services.AddScoped<IPermissionService, PermissionService>();

//注册 TokenService JWT生成服务
builder.Services.AddScoped<ITokenService, TokenService>();

//注册DeviceService服务
builder.Services.AddScoped<IDeviceService, DeviceService>();

//注册DevicePointService服务
builder.Services.AddScoped<IDevicePointService, DevicePointService>();

//注册DevicePointRepository仓储服务
builder.Services.AddScoped<IDevicePointRepository, DevicePointRepository>();

//读取 appsettings 里的 "AliOss" 配置段，绑定到 OssOptions 类
builder.Services.Configure<OssOptions>(builder.Configuration.GetSection("AliOss"));

//注册对象存储（IOssFileStorage 的阿里云 OSS 实现）
builder.Services.AddScoped<IOssFileStorage, AliOssFileStorage>();

//注册DeviceRepository 仓储服务
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// 设备采样历史仓储
builder.Services.AddScoped<IDeviceDataRepository, DeviceDataRepository>();

// 设备采样业务服务
builder.Services.AddScoped<IDeviceDataService, DeviceDataService>();

// ==============================
// 6. AI 设备分析（Day44）
// ==============================

// 配置错误在启动阶段暴露，不等用户点击分析才发现缺少密钥。
builder.Services.AddOptions<AiModelOptions>()
    .Bind(builder.Configuration.GetSection(AiModelOptions.Section))
    .ValidateDataAnnotations()
    .Validate(x => Uri.TryCreate(x.Endpoint, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host == "api.deepseek.com"
        && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && uri.AbsolutePath == "/", "本章Endpoint必须为DeepSeek官方HTTPS基础地址")
    .ValidateOnStart();

// 仓储与 Service 按 HTTP 请求创建，保持 DbContext 作用域一致。
builder.Services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();
builder.Services.AddScoped<IAiAnalysisService, AiAnalysisService>();

// 连接客户端复用；每次 CompleteAsync 内部仍创建独立 Agent 和 Session。
builder.Services.AddSingleton<IAiTextClient, DeepSeekMafClient>();

// ==============================
// 7. AI 设备调查任务（Day45）
// ==============================

// 仓储、查询、Service 与 MAF Runner 都按 HTTP 请求创建，
// 保证同一次调查使用同一个 DbContext 作用域。
builder.Services.AddScoped<IAiTaskRepository, AiTaskRepository>();
builder.Services.AddScoped<IAiDeviceQuery, AiDeviceQueryService>();
builder.Services.AddScoped<IAiTaskService, AiTaskService>();
builder.Services.AddScoped<IAiTaskRunner, MafTaskRunner>();

// ==============================
// 8. AI 设备知识库（Day46）
// ==============================

// 检索当前用 MySQL 关键词实现；IKnowledgeVectors 保留将来接向量库的扩展点。
builder.Services.Configure<KnowledgeOptions>(
    builder.Configuration.GetSection(KnowledgeOptions.Section));
builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
builder.Services.AddScoped<IKnowledgeSource, LocalKnowledgeSource>();
builder.Services.AddScoped<IKnowledgeVectors, KeywordKnowledgeVectors>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();

// ==============================
// 9. MCP 设备工具权限（Day47）
// ==============================

// 工具目录与执行入口都在 McpAccessService；
// 本章不引入 ModelContextProtocol 包，也不暴露 /mcp 协议端点。
builder.Services.AddScoped<IMcpRepository, McpRepository>();
builder.Services.AddScoped<McpAccessService>();

// ==============================
// 10. 设备巡检调度（Day48）
// ==============================

// 计划与队列仓储、调度服务按请求/按轮次创建，保持 DbContext 作用域一致。
builder.Services.AddScoped<IInspectionRepository, InspectionRepository>();
builder.Services.AddScoped<InspectionService>();

// 后台 Worker 是单例，每轮自己 CreateScope 解析 Scoped 服务。
builder.Services.AddHostedService<InspectionWorker>();

// ==============================
// 11. AI 告警研判与通知分发（Day49）
// ==============================

// 默认 Notify:Enabled=false：只落 Outbox 待办、不发真实请求。
builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.Section));
builder.Services.AddScoped<IAdvisoryRepository, AdvisoryRepository>();
builder.Services.AddScoped<AdvisoryService>();
builder.Services.AddScoped<INotificationSender, WebhookNotificationSender>();

// 15 秒超时 + 禁止自动重定向，避免带签名的请求被转发到意外主机。
builder.Services.AddHttpClient("Notify", c => c.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddHostedService<NotificationWorker>();

// ==============================
// 12. 控制申请人工审批与审计（Day50）
// ==============================

// 本章只做审批与审计，不下发任何设备命令。
builder.Services.AddScoped<IControlRepository, ControlRepository>();
builder.Services.AddScoped<IControlRequestService, ControlRequestService>();

// ==============================
// 4.串口诊断（Day12）
// ==============================

// 注册白名单配置：默认只允许 COM11
builder.Services.AddSingleton(new SerialDiagnosticsOptions());

// 注册串口诊断服务。实现放在 DeviceGateway，
// 因为它要引用 System.IO.Ports 真正打开 COM 口。
builder.Services.AddScoped<ISerialDiagnosticService, SerialDiagnosticService>();

// Modbus 诊断白名单（默认只允许 127.0.0.1 / localhost）
builder.Services.AddSingleton(new ModbusDiagnosticsOptions());

// Modbus 诊断服务（实现在 DeviceGateway，因为它要用 NModbus）
builder.Services.AddScoped<IModbusDiagnosticService, ModbusDiagnosticService>();


// 自动扫描包含 DeviceMappingProfile 的程序集，注册 AutoMapper 服务
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(IotGatewayLearning.Application.Mappings.DeviceMappingProfile).Assembly);
});


// ==============================
// 5. MQTT 诊断（Day14）
// ==============================

// MQTT 诊断白名单（默认只允许 127.0.0.1 / localhost / ::1）
builder.Services.AddSingleton(new MqttDiagnosticsOptions());

// MQTT 诊断服务（实现在 DeviceGateway，因为它要用 MQTTnet）
builder.Services.AddScoped<IMqttDiagnosticService, MqttDiagnosticService>();

// ==============================
// 4.WebAPI基础服务
// ==============================

builder.Services.AddControllers();

// ==============================
// 表单上传的"缓冲"设置
//
// ⚠️ 为什么必须加这一段：
//    ASP.NET Core 在解析 multipart（文件上传）时，如果内容超过 MemoryBufferThreshold
//    （默认只有 64KB），它会先把内容缓存到【临时目录】(%TEMP%)。
//    而【这台机器禁止写用户目录】→ 上传稍大一点的照片就会报：
//        UnauthorizedAccessException:
//        Access to the path '...\AppData\Local\Temp\ASPNETCORE_xxx.tmp' is denied.
//    把阈值提到 10MB，10MB 以内的上传就全部留在内存里，不碰临时目录。
//    （小图片能传成功、大图片失败，就是这个原因）
// ==============================
builder.Services.Configure<FormOptions>(options =>
{
    options.MemoryBufferThreshold = 10 * 1024 * 1024;      // 10MB 以内放内存，不落临时文件
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;   // 整个表单最大 10MB
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// 配置 SwaggerGen：加载 API 层与 Application 层的 XML 注释文件
builder.Services.AddSwaggerGen(options =>
{
    // -------------------------------------------------------------
    // 1. 加载 API 层的 XML 注释（包含 Controllers 接口路由与方法注释）
    // -------------------------------------------------------------
    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    if (File.Exists(apiXmlPath))
    {
        // includeControllerXmlComments: true 表示连同 Controller 类名顶部的注释也一并载入
        options.IncludeXmlComments(apiXmlPath, includeControllerXmlComments: true);
    }
    // -------------------------------------------------------------
    // 2. 加载 Application 层的 XML 注释（包含 DTO 请求参数与响应实体字段注释）
    // -------------------------------------------------------------
    var applicationXmlFile = "IotGatewayLearning.Application.xml";
    var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXmlFile);
    if (File.Exists(applicationXmlPath))
    {
        options.IncludeXmlComments(applicationXmlPath);
    }
});

var app = builder.Build();


// ==============================
// 5.HTTP请求管道
// ==============================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ==============================
// HTTP 请求日志
// ==============================
app.UseSerilogRequestLogging(); //自动记录 HTTP 请求

// ==============================
// 全局异常处理中间件
// ==============================
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();  //确保客户端使用 HTTPS,如果http请求是http不是https,那么不会执行后面流程，会Middleware 短路，直接结束。

app.UseAuthentication();    //JWT：认证 ， 认证完之后假如jwt合法，读取Claims,创建 ClaimsPrincipal,放入 HttpContext.User,得到User信息。
app.UseAuthorization();     //JWT：授权

app.MapControllers();       //找到真正处理请求的 Controller

app.Run();
