using IotGatewayLearning.Infrastructure.Data;
using IotGatewayLearningApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;
using Serilog;
using System.Text;
using System.Security.Claims;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using IotGatewayLearningApi.Security;
using IotGatewayLearning.Infrastructure.Repositories;
using IotGatewayLearning.Application.Services;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Application.Services.Implementations;


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

//注册AdminService 管理员服务
builder.Services.AddScoped<IAdminService, AdminService>();

//注册 TokenService JWT生成服务
builder.Services.AddScoped<ITokenService, TokenService>();


// ==============================
// 4.WebAPI基础服务
// ==============================

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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
