##### Day49 实现规格：AI 告警研判、维修建议与通知分发

来源：`docs-v8/Day49-告警研判维修建议与通知分发.md`。以下为代码契约，照抄原文。

##### 1. 功能清单

###### 1.1 创建告警研判与维修建议

`POST /api/ai/advisories`

请求 JSON：
```json
{
  "runId": "39f16e5689bb43ba881b9872e7ba347a",
  "knowledgeAnswerId": 12
}
```
响应 JSON：
```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "id": "advisory-example",
    "state": "Active",
    "summary": "温度持续上升，建议人工核查",
    "advice": "检查散热并核对现场工况"
  }
}
```
###### 1.2 查询 AI 告警与维修建议

`GET /api/ai/advisories`，不发送 JSON 请求体。

响应 JSON：
```json
{
  "code": 200,
  "message": "成功",
  "data": []
}
```
###### 1.3 确认已知悉 AI 告警

`POST /api/ai/advisories/advisory-example/handle`

请求 JSON：
```json
{
  "close": false,
  "remark": "已通知值班工程师核查"
}
```
响应 JSON：
```json
{
  "code": 200,
  "message": "成功",
  "data": true
}
```
###### 1.4 关闭已处理的 AI 告警

`POST /api/ai/advisories/advisory-example/handle`

请求 JSON：
```json
{
  "close": true,
  "remark": "现场清理滤网，温度趋势已人工核对"
}
```
响应 JSON：
```json
{
  "code": 200,
  "message": "成功",
  "data": true
}
```
###### 1.5 查询通知投递状态

`GET /api/ai/advisories/notifications`，不发送 JSON 请求体。

响应 JSON：
```json
{
  "code": 200,
  "message": "成功",
  "data": []
}
```
##### 2. SQL DDL
```sql
CREATE TABLE ai_advisories (
  Id CHAR(32) PRIMARY KEY,
  RunId CHAR(32) NOT NULL UNIQUE,
  Owner VARCHAR(50) NOT NULL,
  DeviceCode VARCHAR(50) NOT NULL,
  State VARCHAR(30) NOT NULL,
  EvidenceJson LONGTEXT NOT NULL,
  Summary VARCHAR(1000) NOT NULL,
  Advice VARCHAR(2000) NOT NULL,
  Remark VARCHAR(500) NOT NULL,
  HandledBy VARCHAR(50) NOT NULL,
  CreatedAt DATETIME(6) NOT NULL,
  UpdatedAt DATETIME(6) NOT NULL
);
CREATE TABLE ai_notification_outbox (
  Id CHAR(32) PRIMARY KEY,
  AdvisoryId CHAR(32) NOT NULL UNIQUE,
  State VARCHAR(30) NOT NULL,
  Attempts INT NOT NULL,
  NextAt DATETIME(6) NOT NULL,
  LastError VARCHAR(200) NOT NULL,
  UpdatedAt DATETIME(6) NOT NULL
);
```
##### 3. 实体字段表
`AiAdvisory`（表 `ai_advisories`，全部 NOT NULL）：

| C# 属性 | 类型 | 列名 | 长度 | C# 默认值 |
|---|---|---|---|---|
| Id | string | Id | 32 | `Guid.NewGuid().ToString("N")` |
| RunId | string | RunId | 32 | `""` |
| Owner | string | Owner | 50 | `""` |
| DeviceCode | string | DeviceCode | 50 | `""` |
| State | string | State | 30 | `"Running"` |
| EvidenceJson | string | EvidenceJson | LONGTEXT | `""` |
| Summary | string | Summary | 1000 | `""` |
| Advice | string | Advice | 2000 | `""` |
| Remark | string | Remark | 500 | `""` |
| HandledBy | string | HandledBy | 50 | `""` |
| CreatedAt | DateTime | CreatedAt | DATETIME(6) | `DateTime.UtcNow` |
| UpdatedAt | DateTime | UpdatedAt | DATETIME(6) | `DateTime.UtcNow` |

`AiNotification`（表 `ai_notification_outbox`，全部 NOT NULL）：

| C# 属性 | 类型 | 列名 | 长度 | C# 默认值 |
|---|---|---|---|---|
| Id | string | Id | 32 | `Guid.NewGuid().ToString("N")` |
| AdvisoryId | string | AdvisoryId | 32 | `""` |
| State | string | State | 30 | `"Pending"` |
| Attempts | int | Attempts | INT | `0` |
| NextAt | DateTime | NextAt | DATETIME(6) | `DateTime.UtcNow` |
| LastError | string | LastError | 200 | `""` |
| UpdatedAt | DateTime | UpdatedAt | DATETIME(6) | `DateTime.UtcNow` |

##### 4. 仓储方法签名
```csharp
public sealed class AdvisoryRepository(AppDbContext db)
Task<AiNotification?> ClaimAsync(CancellationToken ct)
Task<AiAdvisory> GetAsync(string id, CancellationToken ct)
Task DeliveredAsync(AiNotification n, string state, string error, CancellationToken ct)
Task<AiAdvisory?> ByRunAsync(string runId, CancellationToken ct)
Task<object> EvidenceAsync(InspectionRun run, long? answerId, CancellationToken ct)
Task<bool> StartAsync(AiAdvisory row, CancellationToken ct)
Task CompleteAsync(AiAdvisory row, bool notify, CancellationToken ct)
Task<List<AiAdvisory>> ListAsync(string owner, CancellationToken ct)
Task<bool> HandleAsync(string id, string owner, bool close, string remark, CancellationToken ct)
Task<List<AiNotification>> NotificationsAsync(string owner, CancellationToken ct)
```

- `ClaimAsync`：先将 `State=="Sending"` 且 `UpdatedAt < UtcNow-2min` 的记录置 `Unknown`；再取 `Pending/Retry` 且 `NextAt <= UtcNow` 按 `NextAt` 升序首条；条件更新 `Pending/Retry -> Sending`、`Attempts+1`、`UpdatedAt=UtcNow`；`changed != 1` 返回 `null`。
- `DeliveredAsync`：仅更新 `Id==n.Id && State=="Sending"`；写 `State`、`LastError`、`NextAt=UtcNow.AddSeconds(30 * n.Attempts)`、`UpdatedAt=UtcNow`。
- `StartAsync`：`db.Add(row)` + `SaveChangesAsync`；捕获 `DbUpdateException` 内层 `MySqlException Number:1062`，`Detach` 后返回 `false`。
- `CompleteAsync`：`notify` 为 true 时 `db.Add(new AiNotification { AdvisoryId = row.Id })`；设置 `row.UpdatedAt`；一次 `SaveChangesAsync` 提交（同一事务）。
- `HandleAsync`：条件 `Id==id && Owner==owner && (close ? State=="Acknowledged" : State=="Active")`；`ExecuteUpdateAsync` 写 `State=close?"Closed":"Acknowledged"`、`Remark`、`HandledBy=owner`、`UpdatedAt`；返回受影响行数 `== 1`。
- `EvidenceAsync`：返回 `new { run.Id, run.DeviceCode, run.EvidenceJson, run.Answer, knowledge, knowledgeMissing = knowledge is null, repairs }`。

##### 5. 业务规则
- Day31 阈值规则负责确定性告警；本章 AI 告警是补充研判，来源标记 AI，不能替代 PLC 联锁，也不能把模型说“正常”用于自动恢复原规则告警。新建 `ai_advisories` 保存 AI 提示，不向 Day31 要求 `rule_id` 的 `alarms` 表随意插入空规则。
- 状态机：`Running`（研判中）→ `Active`（需关注，已产生通知待办）/ `NoAttention`（不需关注，不发通知）/ `Failed`（模型失败）；`Active` → `Acknowledged`（确认知悉）；`Acknowledged` → `Closed`（关闭）。`Failed` 绝不等于 `NoAttention`；`Closed` 不能改回 `Acknowledged`。
- 确认前置：仅 `Active`；`remark` 非空且 `Length <= 500`；写 `HandledBy`、`Remark`、`UpdatedAt`；不满足返回 409。
- 关闭前置：仅 `Acknowledged`；`remark` 非空且 `Length <= 500`；`Active` 直接关闭返回 409。关闭不恢复 Day31 规则告警，不向 PLC 发送复位命令。
- 确认/关闭复用同一 Action、`Service.HandleAsync`、`Repository.HandleAsync`，`close=true` 走 `Acknowledged→Closed` 分支，没有第二个同名接口。
- Outbox 事务边界：`StartAsync` 先落 `Running` 占住 RunId 唯一键；模型返回后 `CompleteAsync` 在同一个 `SaveChanges` 事务中提交研判与通知待办；`notify=false` 不建待办。
- 同一 RunId 只保留一次研判（数据库唯一键）；重复提交返回旧记录，包括 `Failed`；需要新研判先生成新的巡检证据。
- 幂等键：`ai_advisories.RunId` 唯一；`ai_notification_outbox.AdvisoryId` 唯一，每条研判至多一条待办。外部钉钉无本项目事务，网络超时仍可能重复通知，消息包含 AdvisoryId 便于识别。
- 模型输出契约：只返回 JSON 对象，`needsAttention` 为 boolean、`summary` 为 string、`advice` 为 string；缺字段、超长、不是 JSON 都记 `Failed`，不擅自默认 `needsAttention=false`。
- 长度校验：`summary` 1–1000，`advice` 1–2000，越界抛 `InvalidOperationException("模型输出长度不合规")`。
- 模型失败分支：`notify=false`、`State="Failed"`、`Summary="研判未完成，请核对证据及模型配置"`、`Advice=""`；通知不能在失败时误发正常结论。
- 模型调用超时 120 秒（`CancelAfter`）；落库使用独立 5 秒 `CancellationTokenSource`，即使执行被取消也尽量写回终态。
- 投递重试：`Attempts < 3` 记 `Retry` 等下轮；`Attempts >= 3` 记 `Failed` 不再自动重发；退避 `NextAt = UtcNow.AddSeconds(30 * Attempts)`；Worker 轮询间隔 10 秒。
- 抢占防重：`Sending` 超过 2 分钟视为 `Unknown`，保留而不无条件重发；只有 `Pending/Retry` 且 `NextAt` 到期才可抢占。
- 通知状态：`Accepted` 只表示第三方接收，不能显示成“人员已读”；`Failed`/`Unknown` 必须保留错误和次数；通知失败重试不再次调用模型，不通过重做研判来重发通知。
- 证据来源：Run 必须 `State=="Succeeded"` 且 `EvidenceJson` 非空；`knowledgeAnswerId` 可为 null（明确告知模型资料缺失）；提供时必须是本人、`State=="Succeeded"` 的 Day46 回答。
- 引用校验：Citations 不能为空；每个引用文档必须 `DeviceId==当前设备`、`Owner==run.Owner`、`State=="Ready"`、`Revision==c.Revision`，否则抛 `"引用不适用当前设备"`。
- 维修记录：按 `DeviceId` 取 `OrderByDescending(Id).Take(5)`，字段 `{ Id, Title, Description, Status }`；维修内容属于资料，不作为系统指令。
- 权限：创建前调用 `access.InvokeAsync(actor, new(run.DeviceCode, "get_device"), ct)`，旧证据 ID 不能成为越权凭证；列表、确认、关闭、通知查询均按 JWT `actor` 的 `Owner` 过滤。
- 查询上限：研判列表按 `CreatedAt` 倒序 `Take(100)`；通知列表按 `UpdatedAt` 倒序 `Take(100)`。
- 错误码：404 `"巡检记录不存在"`；400 `"需要已完成且有证据的巡检"`；400 `"需要处理备注，最多500字"`；409 `"记录不存在或状态已变化，请刷新"`。统一 `ApiResponse<T>` 包装。
- 时间统一 UTC、`datetime(6)`；`point_code` 长度 80（项目既有约定，本章无新增字段）。

##### 6. 通知发送
钉钉配置键：`DingTalk:Webhook`（完整 `https://oapi.dingtalk.com/robot/send` 地址）、`DingTalk:Secret`（加签密钥，可空）。未配置时任务进入失败重试，不静默声称发送成功。

- `DingTalkSender(IHttpClientFactory clients, IConfiguration configuration)`；Webhook / Secret 只从服务端 `IConfiguration` 读取，WPF、SQL 截图、模型 Prompt 均不得出现。
- URL 白名单：`uri.Scheme == "https"`、`uri.Host == "oapi.dingtalk.com"`、`uri.AbsolutePath == "/robot/send"`，任一不符抛 `InvalidOperationException`。
- 加签：`timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`；`HMACSHA256(secret)` 对 `timestamp + "\n" + secret` 计算，Base64 后 `Uri.EscapeDataString`，追加 `&timestamp=...&sign=...`。
- 命名 HttpClient `"DingTalk"`：`Timeout = 15s`、`AllowAutoRedirect = false`。
- 请求体：`new { msgtype = "text", text = new { content = text } }`；`EnsureSuccessStatusCode()` 后必须解析响应体 `errcode`，`errcode != 0` 抛 `InvalidOperationException("第三方拒绝通知")`（HTTP 200 不等于业务成功）。
- 消息文本：`$"AI提示 {a.Id}\n设备：{a.DeviceCode}\n{a.Summary}\n请进入平台查看证据。"`；不外发手册全文、采样全集，仅发事件编号和摘要。
- 最小替代方案（无钉钉配置）：使用可配置 webhook + 默认只落 Outbox 不真发。新增 `Notify:Enabled`（默认 `false`）、`Notify:Webhook`、`Notify:Secret`、`Notify:AllowedHosts`（逗号分隔主机白名单）。`Notify:Enabled=false` 时 Worker 不抢占、不发请求，待办保持 `Pending`，不增加 `Attempts`，WPF 仍可查询；启用后使用通用 HTTPS webhook（请求体 `{ "text": "<消息>" }`，主机必须在白名单），失败按 <3 次 `Retry` / >=3 次 `Failed` 与 `30 * Attempts` 秒退避；钉钉配置存在时优先走钉钉 sender。

##### 7. DI 注册与配置项
`AppDbContext` 追加 `Entity<AiAdvisory>()` 和 `Entity<AiNotification>()`，分别配置 `RunId`、`AdvisoryId` 唯一索引，与 SQL 保持一致。

```csharp
// 类型来自本章Infrastructure.AI.Advisories与WebAPI.AI.Advisories。
builder.Services.AddScoped<AdvisoryRepository>();
builder.Services.AddScoped<AdvisoryService>();
builder.Services.AddScoped<DingTalkSender>();
builder.Services.AddHttpClient("DingTalk", c => c.Timeout = TimeSpan.FromSeconds(15))
    // 禁止第三方重定向导致带Token地址被转发到意外主机。
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

// 以下放到现有AddQuartz(q => { ... })内部。
var notificationKey = new Quartz.JobKey("ai-notification", "ai");
q.AddJob<NotificationJob>(o => o.WithIdentity(notificationKey));
q.AddTrigger(o => o.ForJob(notificationKey).WithIdentity("ai-notification-trigger", "ai")
    .WithSimpleSchedule(s => s.WithIntervalInSeconds(10).RepeatForever()));
```

控制器契约：

```csharp
[ApiController, Route("api/ai/advisories"), Authorize(Policy = "user:manage")]
public sealed class AdvisoriesController(AdvisoryService service) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? throw new UnauthorizedAccessException();
    [HttpPost] public async Task<object> Create(CreateAdvisory r, CancellationToken ct);
    [HttpGet] public async Task<object> List(CancellationToken ct);
    [HttpPost("{id}/handle")] public async Task<object> Handle(string id, HandleAdvisory r, CancellationToken ct);
    [HttpGet("notifications")] public async Task<object> Notifications(CancellationToken ct);
}
```

WPF 注册：

```csharp
containerRegistry.Register<IotGateway.WPF.Services.AdvisoryApiClient>();
containerRegistry.RegisterForNavigation<IotGateway.WPF.Views.AdvisoryView,
    IotGateway.WPF.ViewModels.AdvisoryViewModel>();

new MenuItemModel {Title="AI告警研判",ViewName="AdvisoryView",RequiredPermission="user:manage"},
```

配置项汇总：`DingTalk:Webhook`、`DingTalk:Secret`、`Notify:Enabled`（默认 false）、`Notify:Webhook`、`Notify:Secret`、`Notify:AllowedHosts`。后台权限以 Controller 与 Service 为准，隐藏按钮不构成授权。
