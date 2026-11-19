##### Day47 实现规格：MCP 设备工具接入与权限管理

> 来源：`docs-v8/Day47-MCP设备工具接入与权限管理.md`。只提取契约；与本项目既有命名的差异见 7.3。

###### 1. 功能清单

###### 1.1 路由与页面入口
| # | 功能 | HTTP / 路由 | 页面入口 |
|---|---|---|---|
| 1 | 查询可用设备工具 | `GET /api/mcp-admin/tools` | MCP工具管理 |
| 2 | 查询设备工具授权 | `GET /api/mcp-admin/grants` | MCP工具管理 |
| 3 | 授予设备工具权限 | `PUT /api/mcp-admin/grants` | MCP工具管理 |
| 4 | 撤销设备工具权限 | `POST /api/mcp-admin/grants/revoke` | MCP工具管理 |
| 5 | 试调用设备工具 | `POST /api/mcp-admin/invoke` | MCP工具管理 |
| 6 | 查询授权变更记录 | `GET /api/mcp-admin/changes` | MCP工具管理 |
| 7 | 查询工具调用记录 | `GET /api/mcp-admin/calls` | MCP工具管理 |
| 8 | 查看单次工具调用详情 | `GET /api/mcp-admin/calls/{id}` | MCP工具管理 |
| - | 真正 MCP 端点 | `/mcp`（SDK 处理 JSON-RPC） | 无 |

###### 1.2 通用约定
- 管理接口：JWT 认证 + `[Authorize(Policy = "user:manage")]`；`/mcp`：JWT 认证；设备级权限在工具执行内再查。
- 成功包络：`{"code":200,"message":"成功","data":...}`；失败包络：`code` 等于 HTTP 状态码，`data` 为 `null`。
- 错误码：参数/白名单 400；未认证 401（认证中间件拦截，不进入 Service）；无设备工具授权 403；调用记录不存在 404；工具读取失败 502。

###### 1.3 各功能请求与响应
**功能 1 查询可用设备工具** `GET /api/mcp-admin/tools`，无 JSON 请求体。
```json
{"code":200,"message":"成功","data":[{"name":"get_device","description":"查询设备资料，不执行控制","arguments":"deviceCode:string"}]}
```
**功能 2 查询设备工具授权** `GET /api/mcp-admin/grants`，无 JSON 请求体。
```json
{"code":200,"message":"成功","data":[]}
```
**功能 3 授予设备工具权限** `PUT /api/mcp-admin/grants`，请求体：
```json
{"actor":"admin","deviceCode":"PLC-001","tool":"get_history"}
```
响应：
```json
{"code":200,"message":"成功","data":true}
```
**功能 4 撤销设备工具权限** `POST /api/mcp-admin/grants/revoke`，请求体：
```json
{"actor":"admin","deviceCode":"PLC-001","tool":"get_history"}
```
响应：
```json
{"code":200,"message":"成功","data":true}
```
**功能 5 试调用设备工具** `POST /api/mcp-admin/invoke`，请求体：
```json
{"deviceCode":"PLC-001","tool":"get_history","pointCode":"temperature"}
```
响应：
```json
{"code":200,"message":"成功","data":{"callId":"3f7f2c5fdb2541e480dbf5b8e52c963ba9","status":"Succeeded","data":{"device":"PLC-001","point":"temperature","windowEnd":"2026-09-14T03:00:00Z","truncated":false,"samples":[{"id":891,"valueDecimal":25.6,"unit":"C","quality":"Good","occurredAt":"2026-09-14T02:59:55Z"}]}}}
```
`callId` 也可示例为 `"call-example"`，`data` 也可为 `{"samples":[]}`；`status` 取 `Succeeded`/`Denied`/`Failed`/`Canceled`，`Started` 不出现在响应。
**功能 6 查询授权变更记录** `GET /api/mcp-admin/changes`，无 JSON 请求体。
```json
{"code":200,"message":"成功","data":[]}
```
**功能 7 查询工具调用记录** `GET /api/mcp-admin/calls`，无 JSON 请求体。
```json
{"code":200,"message":"成功","data":[]}
```
**功能 8 查看单次工具调用详情** `GET /api/mcp-admin/calls/{id}`，无 JSON 请求体。
```json
{"code":200,"message":"成功","data":{"id":"call-example","actor":"admin","deviceCode":"PLC-001","tool":"get_history","status":"Succeeded","argumentsJson":"{}","resultJson":"{}"}}
```

###### 2. SQL DDL
```sql
-- 一条授权只允许某个账号调用某台设备的某个工具。
CREATE TABLE ai_mcp_grants (
    Actor VARCHAR(50) COLLATE utf8mb4_bin NOT NULL,
    DeviceCode VARCHAR(50) COLLATE utf8mb4_bin NOT NULL,
    Tool VARCHAR(50) COLLATE utf8mb4_bin NOT NULL,
    GrantedBy VARCHAR(50) NOT NULL,
    GrantedAt DATETIME(6) NOT NULL,
    PRIMARY KEY (Actor, DeviceCode, Tool)
);

-- Started代表已经进入业务执行边界；只有拿到真实返回才改为Succeeded。
CREATE TABLE ai_mcp_calls (
    Id CHAR(32) PRIMARY KEY,
    Actor VARCHAR(50) NOT NULL,
    DeviceCode VARCHAR(50) NOT NULL,
    Tool VARCHAR(50) NOT NULL,
    ArgumentsJson JSON NOT NULL,
    Status VARCHAR(30) NOT NULL,
    ResultJson LONGTEXT NOT NULL,
    StartedAt DATETIME(6) NOT NULL,
    CompletedAt DATETIME(6) NULL,
    INDEX ix_mcp_actor_time (Actor, StartedAt)
);

-- 授权变更单独记录，撤销授权不能抹掉历史审计。
CREATE TABLE ai_mcp_grant_changes (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    Operator VARCHAR(50) NOT NULL,
    Actor VARCHAR(50) NOT NULL,
    DeviceCode VARCHAR(50) NOT NULL,
    Tool VARCHAR(50) NOT NULL,
    Action VARCHAR(20) NOT NULL,
    OccurredAt DATETIME(6) NOT NULL
);
```
不插入默认全权限授权；用 Day04 已有账号、Day07 已有设备显式授权。若允许账号改名，需全平台迁移为不可变用户 ID，不能只改这一张表。

###### 3. 实体字段表

###### 3.1 `McpGrant`（`ai_mcp_grants`，联合主键 `Actor+DeviceCode+Tool`）
| C# 属性 | C# 类型 | 数据库列 | 类型/长度 | 默认值 |
|---|---|---|---|---|
| `Actor` | `string` | `Actor` | `VARCHAR(50) COLLATE utf8mb4_bin NOT NULL` | `""` |
| `DeviceCode` | `string` | `DeviceCode` | `VARCHAR(50) COLLATE utf8mb4_bin NOT NULL` | `""` |
| `Tool` | `string` | `Tool` | `VARCHAR(50) COLLATE utf8mb4_bin NOT NULL` | `""` |
| `GrantedBy` | `string` | `GrantedBy` | `VARCHAR(50) NOT NULL` | `""` |
| `GrantedAt` | `DateTime` | `GrantedAt` | `DATETIME(6) NOT NULL` | 无 |

###### 3.2 `McpCall`（`ai_mcp_calls`，索引 `ix_mcp_actor_time(Actor,StartedAt)`）
| C# 属性 | C# 类型 | 数据库列 | 类型/长度 | 默认值 |
|---|---|---|---|---|
| `Id` | `string` | `Id` | `CHAR(32) PRIMARY KEY` | `Guid.NewGuid().ToString("N")` |
| `Actor` | `string` | `Actor` | `VARCHAR(50) NOT NULL` | `""` |
| `DeviceCode` | `string` | `DeviceCode` | `VARCHAR(50) NOT NULL` | `""` |
| `Tool` | `string` | `Tool` | `VARCHAR(50) NOT NULL` | `""` |
| `ArgumentsJson` | `string` | `ArgumentsJson` | `JSON NOT NULL` | `"{}"` |
| `Status` | `string` | `Status` | `VARCHAR(30) NOT NULL` | `"Started"` |
| `ResultJson` | `string` | `ResultJson` | `LONGTEXT NOT NULL` | `""` |
| `StartedAt` | `DateTime` | `StartedAt` | `DATETIME(6) NOT NULL` | `DateTime.UtcNow` |
| `CompletedAt` | `DateTime?` | `CompletedAt` | `DATETIME(6) NULL` | `null` |

###### 3.3 `McpGrantChange`（`ai_mcp_grant_changes`）
| C# 属性 | C# 类型 | 数据库列 | 类型/长度 | 默认值 |
|---|---|---|---|---|
| `Id` | `long` | `Id` | `BIGINT PRIMARY KEY AUTO_INCREMENT` | 自增 |
| `Operator` | `string` | `Operator` | `VARCHAR(50) NOT NULL` | `""` |
| `Actor` | `string` | `Actor` | `VARCHAR(50) NOT NULL` | `""` |
| `DeviceCode` | `string` | `DeviceCode` | `VARCHAR(50) NOT NULL` | `""` |
| `Tool` | `string` | `Tool` | `VARCHAR(50) NOT NULL` | `""` |
| `Action` | `string` | `Action` | `VARCHAR(20) NOT NULL` | `""` |
| `OccurredAt` | `DateTime` | `OccurredAt` | `DATETIME(6) NOT NULL` | `DateTime.UtcNow` |

**3.4 契约记录**：`GrantRequest(Actor, DeviceCode, Tool)`、`InvokeRequest(DeviceCode, Tool, PointCode="temperature")`、`ToolInfo(Name, Description, Arguments)`、`McpReadResult(CallId, Status, Data)`；前两者字段均为 `[Required, StringLength(50)]`，`PointCode` 默认 `temperature`。DbContext 追加 `modelBuilder.Entity<McpGrant>().HasKey(x => new { x.Actor, x.DeviceCode, x.Tool });`，并注册 `McpCall`、`McpGrantChange`。

###### 4. 仓储与服务签名
```csharp
public interface IMcpRepository
{
    Task<bool> ActorExistsAsync(string actor, CancellationToken ct);
    Task<bool> DeviceExistsAsync(string device, CancellationToken ct);
    Task<bool> AllowedAsync(GrantRequest request, CancellationToken ct);
    Task ChangeAsync(GrantRequest request, string actor, bool grant, CancellationToken ct);
    Task<List<McpGrant>> GrantsAsync(CancellationToken ct);
    Task<List<McpGrantChange>> ChangesAsync(CancellationToken ct);
    Task AddCallAsync(McpCall call, CancellationToken ct);
    Task CompleteAsync(string id, string status, string result, CancellationToken ct);
    Task<List<McpCall>> CallsAsync(CancellationToken ct);
    Task<McpCall?> CallAsync(string id, CancellationToken ct);
}
```
Service 公共方法：`GrantsAsync(ct)`、`ChangeAsync(GrantRequest r, string actor, bool grant, ct)`、`InvokeAsync(string actor, InvokeRequest r, ct)`、`ChangesAsync(ct)`、`CallsAsync(ct)`、`CallAsync(string id, ct)`；`Catalog` 为 `static readonly ToolInfo[]`。`IAiDeviceQuery` 复用 Day45：`DeviceAsync(string device, ct)`、`HistoryAsync(string device, string point, DateTime end, ct)`、`AlarmsAsync(string device, ct)`，实现为 `AiDeviceQueryService`。

###### 5. 业务规则
- 工具白名单固定 3 个：`get_device`、`get_history`、`get_alarms`；`Catalog` 是 `static readonly ToolInfo[]`，写死在代码里。
- 授权请求 `actor`、`deviceCode`、`tool` 各 1~50 字符；`actor` 必须存在且启用；`deviceCode` 必须存在且启用；`tool` 不在白名单直接 400。
- `actor` 只从 JWT / `HttpContext.User.Identity.Name` 取，绝不作为工具参数；管理操作员与被授权人是两个身份。
- 存在性检查：`users.username={actor} AND status=1`；`devices.device_code={device} AND is_deleted=0 AND status=1`。
- 授予：`INSERT ... ON DUPLICATE KEY UPDATE GrantedBy={actor},GrantedAt={now}`；重复授权只更新时间与操作员，不新增重复三元组。
- 撤销：删除精确三元组，允许清理已停用账号或设备的遗留授权；不承诺中止已在途的外部请求；重复撤销不报错但仍记录变更。
- 授权变更与变更审计在同一个数据库事务内完成；`Action` 取 `Grant`/`Revoke`。
- 每次 `tools/call` 都重新检查白名单、账号存在性、三元组授权；目录可见不等于任何设备可读；读操作开始前检查即可，设备写操作必须在实际下发前重新检查授权、审批和设备状态（Day50、Day51）。
- `InvokeAsync` 顺序固定：参数校验 → `AddCallAsync` 写 `Started` → 白名单/账号/授权检查 → 调 `IAiDeviceQuery` → `CompleteAsync(Succeeded)`。
- 审计先写 `Started` 再查权限：即使被拒绝也落库，管理员能看到谁在尝试越权；反过来先查权限会导致拒绝请求无痕迹。
- 异常终态：`UnauthorizedAccessException` → `Denied` + 对外 403；`OperationCanceledException` → `Canceled` 并原样抛出；其他异常 → `Failed` + 对外 502，消息带 `callId`。
- 请求取消时审计用独立 `CancellationTokenSource(TimeSpan.FromSeconds(5))`，不无限等待。
- `Started` 只表示已进入业务执行边界；进程崩溃可留下 `Started`，页面不得自动显示成功；长期运行可加超时巡检标 `Unknown`，不得推测数据已返回。
- 试调用输入：`actor`、`deviceCode`、`tool` 非空且 ≤50；`PointCode` 非空且 ≤50（文档值；本项目 `point_code` 既有约定 80，落地取 80 并同步 `StringLength(80)` 与 `>80` 判断）。
- 未通过 JWT 的请求由认证中间件返回 401，尚未进入 Service，不伪造 `Started` 审计。
- `get_history` 固定最近 1 分钟窗口，最多取 61 条、返回 60 条，超过标记 `truncated=true`，样本含 `quality`、`occurredAt`；`get_alarms` 空结果不等于设备一定正常；空数组不等于设备安全。
- 本章只注册只读工具，不注册写工具；不接受用户上传 C# 脚本、任意 SQL、任意远程地址、任意工具名；`get_alarms` 不自动确认告警。
- 管理端各取最近 100 条：授权按 `GrantedAt` 倒序、变更按 `Id` 倒序、调用按 `StartedAt` 倒序。
- `ArgumentsJson` 只保存允许参数，不保存 JWT、连接串、模型密钥；`ResultJson` 保存真实返回，不是模型描述；同一三元组提交两次授权只保留一条，变更日志仍记录每次操作；客户端 `approvedNames` HashSet（`get_device`、`get_history`、`get_alarms`）是第二道防线，服务端 `Catalog` 是最后一道。
- 授权失败 403；调用记录不存在 404；MCP 调用 HTTP 200 也可能 `isError=true`，不能只看 HTTP 状态。

###### 6. MCP 接入

###### 6.1 包与版本
| 包 | 安装项目 | 用途 |
|---|---|---|
| `ModelContextProtocol.AspNetCore` 2.2.0 | 当前 WebAPI 及最小 HTTP 示例 | MCP HTTP 服务端 |
| `ModelContextProtocol` 2.2.0 | Client 项目 | Client、工具对象和托管支持 |
| `Microsoft.Agents.AI.OpenAI` | Day44 助手项目 | MAF 连接 DeepSeek，沿用 Day44 版本 |

###### 6.2 工具注册方式
- `builder.Services.AddMcpServer().WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless).WithTools<DeviceMcpTools>();`
- `app.MapMcp("/mcp").RequireAuthorization();`
- 工具类标 `[McpServerToolType]`；方法标 `[McpServerTool(Name = "get_device")]`、`[Description(...)]`；参数加 `[Description]`；`CancellationToken` 由 SDK 注入。
- 显式注册类，不扫描全部业务方法；`WithResources<PlatformResources>()`、`WithPrompts<DevicePrompts>()` 可选，资源 URI `iot://platform/data-guide`，提示模板 `inspect_device`。
- Stateless：每次 HTTP 请求重新认证，业务状态在 MySQL；协议会话 ID 不是登录令牌；工具类与 Controller 平级，调用 `McpAccessService`，不调用同进程 Controller，不复制设备 Repository。

###### 6.3 认证方式
- `/mcp` 整体 `RequireAuthorization()`，使用本项目私有 JWT；`DeviceMcpTools` 从 `IHttpContextAccessor.HttpContext.User.Identity.Name` 取 `actor`，未认证抛 `UnauthorizedAccessException`。
- 这是私有项目 JWT 集成，不是通用 MCP OAuth：未实现 OAuth 发现、授权服务器、客户端注册、令牌刷新；外部 Host 接入需按 MCP Authorization 规范建设 OAuth 资源服务器。
- Token 只发给配置好的本项目 MCP 地址；Bearer Token 不放进 `tools/call` 参数；部署用 HTTPS、实际 `AllowedHosts` 和有限来源；WPF 不是浏览器，不开放任意 CORS。
- Client 用 `HttpClientTransport`，身份放 `Authorization: Bearer <token>` 头；`MCP_ACCESS_TOKEN` 由当前项目登录接口取得，用环境变量提供，不提交仓库。

###### 6.4 不引入 `ModelContextProtocol` 包的最小替代方案
- 保留 `McpAccessService.Catalog` 作为进程内工具目录；`GET /api/mcp-admin/tools` 直接返回它，不做 `initialize`、`tools/list`。
- 保留 `McpAccessService.InvokeAsync(actor, InvokeRequest, ct)` 作为唯一执行入口；`POST /api/mcp-admin/invoke` 直接调用，`actor` 从 JWT 取。
- 删除 `DeviceMcpTools`、`AddMcpServer`、`WithTools`、`MapMcp` 和包引用；不暴露 `/mcp`，外部 MCP Client 无法连接，只服务进程内 AI Host + HTTP 管理接口。
- 进程内 AI Host 用委托或 `AIFunction` 包装同一服务：`(deviceCode, pointCode) => service.InvokeAsync(actor, new(deviceCode, "get_history", pointCode), ct)`；工具发现直接读 `Catalog`。
- 权限、审计、状态、参数限制全部复用 `McpAccessService`，不得因去掉协议层减少任何检查；HTTP 试调用不是 MCP 协议测试。

###### 7. DI 注册与配置项

###### 7.1 Program.cs 追加
```csharp
using ModelContextProtocol.AspNetCore;
// Build 之前
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMcpRepository, McpRepository>();
builder.Services.AddScoped<McpAccessService>();
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<DeviceMcpTools>();
// Build 之后，沿用现有中间件顺序
app.MapMcp("/mcp").RequireAuthorization();
```
保留 Day45 的 `builder.Services.AddScoped<IAiDeviceQuery, AiDeviceQueryService>();`，不重复注册查询实现。

###### 7.2 配置项
| 配置 | 来源 | 值/说明 |
|---|---|---|
| `MCP_ACCESS_TOKEN` | 环境变量 | 当前项目登录取得的 JWT，不写进 appsettings/仓库 |
| MCP Endpoint | 可信配置/代码常量 | 示例 `https://localhost:7047/mcp`，替换为当前 WebAPI 的 HTTPS 地址 |
| `AllowedHosts` | 配置 | 示例 `localhost;127.0.0.1`；正式部署改实际域名 |
| JWT issuer/audience/key、数据库连接 | 现有配置 | 沿用 Day05 JWT 与现有 `AppDbContext`，不新增 |
| Client 超时 | 代码 | 学习 Client 30 秒；带 JWT Client 90 秒；WPF `RunAsync` 150 秒 |
| 审计兜底超时 / 列表上限 | 代码 | 取消场景独立 5 秒；授权/变更/调用各 100 条 |

###### 7.3 与本项目既有约定的对齐
- 文档 `IotGateway.*` 对应本项目 `IotGatewayLearning.Domain`、`.Application`、`.Infrastructure`，WebAPI 项目为 `IotGatewayLearningApi`；`IAiDeviceQuery` 位于 `IotGatewayLearning.Domain.AI.Tasks`。
- 文档 `ApiResponse<T>.Success(...)` 在本项目不存在；改用 `new ApiResponse<T> { Code = 200, Message = "成功", Data = ... }`。
- 文档 SQL/实体用 PascalCase 列名；本项目既有实体用 snake_case + `[Column]`。严格照文档则保持 PascalCase、实体不加 `[Column]`；贴合项目风格则 DDL/实体统一改 snake_case（`actor`、`device_code`、`tool`、`granted_by`、`granted_at` 等），两者必须整体一致。
- 时间统一 UTC、`datetime(6)`；`point_code` 本项目长度 80（覆盖文档的 50）。
- WPF 沿用 Prism 反射菜单：`McpView` 标 `[MenuItem("MCP工具管理", PackIconKind.*, PermissionCodes.UserManage, <sortOrder>)]`，注册 `McpApiClient` 与 `RegisterForNavigation<McpView, McpViewModel>()`；文档的 `MainViewModel.Catalog` 写法不适用。
- 复用边界：Day45 `DeviceQueryTools` 是任务绑定包装（构造时绑定 device/point、单任务最多 4 次调用、记录 `ToolExecution`），不适合直接注册为 MCP 工具；Day47 新建薄 `DeviceMcpTools` 适配层，复用 `IAiDeviceQuery`/`AiDeviceQueryService`，不重写设备、历史、告警查询逻辑。
