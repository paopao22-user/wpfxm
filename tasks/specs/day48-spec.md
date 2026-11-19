##### Day48 设备巡检计划与异常分析任务 — 实现契约

> 来源：`docs-v8/Day48-设备巡检计划与异常分析任务.md`。分层 Domain / Application / Infrastructure / WebAPI；错误 `AppException(message, statusCode)`；响应 `ApiResponse<T>`；`actor` 只从 JWT 取。

##### 1. 功能清单（HTTP 契约）

**1.1 创建巡检计划** — `POST /api/ai/inspections/plans`（`user:manage`）
```json
// 请求
{"deviceCode":"PLC-001","pointCode":"temperature","question":"说明温度和告警异常证据","intervalSeconds":300}
// 响应
{"code":200,"message":"成功","data":{"id":1,"deviceCode":"PLC-001","pointCode":"temperature","intervalSeconds":300,"enabled":true}}
```
**1.2 查询巡检计划** — `GET /api/ai/inspections/plans`，无请求体
```json
// 响应（当前没有记录）
{"code":200,"message":"成功","data":[]}
```
**1.3 立即执行一次巡检** — `POST /api/ai/inspections/plans/{id}/run`，无请求体
```json
// 响应
{"code":200,"message":"成功","data":{"id":"run-example","planId":1,"state":"Queued","attempts":0,"source":"Manual"}}
```
**1.4 暂停巡检计划** — `PUT /api/ai/inspections/plans/{id}/enabled`
```json
// 请求
false
// 响应
{"code":200,"message":"成功","data":true}
```
**1.5 恢复巡检计划** — `PUT /api/ai/inspections/plans/{id}/enabled`
```json
// 请求
true
// 响应（与 1.4 相同）
{"code":200,"message":"成功","data":true}
```
**1.6 查询巡检任务记录** — `GET /api/ai/inspections/runs`，无请求体
```json
// 响应（当前没有记录）
{"code":200,"message":"成功","data":[]}
// data[] 单条结构
{"id":"39f16e5689bb43ba881b9872e7ba347a","planId":1,"deviceCode":"PLC-001","state":"Queued","attempts":0,"source":"Manual","evidenceJson":"","answer":"","error":""}
```
**1.7 查看巡检证据与回答** — `GET /api/ai/inspections/runs/{id}`，无请求体；不存在或非本人返回 404
```json
// 响应
{"code":200,"message":"成功","data":{"id":"run-example","planId":1,"state":"Succeeded","evidenceJson":"{}","answer":"建议人工核查散热条件","error":""}}
```
**1.8 接收设备事件并提交巡检** — `POST /api/ai/inspections/events`（额外策略 `ai:event:ingest`）
```json
// 请求
{"planId":1,"occurredAt":"2026-09-14T03:05:12Z"}
// 响应
{"code":200,"message":"成功","data":{"id":"event-run-example","planId":1,"state":"Queued","source":"Event"}}
```
##### 2. SQL DDL（逐字照抄）

```sql
CREATE TABLE ai_inspection_plans (
  Id BIGINT PRIMARY KEY AUTO_INCREMENT,
  Owner VARCHAR(50) NOT NULL,
  DeviceCode VARCHAR(50) NOT NULL,
  PointCode VARCHAR(50) NOT NULL,
  Question VARCHAR(1000) NOT NULL,
  IntervalSeconds INT NOT NULL,
  Enabled BOOLEAN NOT NULL,
  NextAt DATETIME(6) NOT NULL
);
CREATE TABLE ai_inspection_runs (
  Id CHAR(32) PRIMARY KEY,
  PlanId BIGINT NOT NULL,
  Owner VARCHAR(50) NOT NULL,
  DeviceCode VARCHAR(50) NOT NULL,
  PointCode VARCHAR(50) NOT NULL,
  Question VARCHAR(1000) NOT NULL,
  MergeKey VARCHAR(120) NOT NULL UNIQUE,
  Source VARCHAR(20) NOT NULL,
  State VARCHAR(30) NOT NULL,
  Attempts INT NOT NULL,
  EvidenceJson LONGTEXT NOT NULL,
  Answer LONGTEXT NOT NULL,
  Error VARCHAR(200) NOT NULL,
  CreatedAt DATETIME(6) NOT NULL,
  NextAt DATETIME(6) NOT NULL,
  LeaseUntil DATETIME(6) NULL,
  CompletedAt DATETIME(6) NULL,
  INDEX ix_inspection_ready(State,NextAt)
);
```
##### 3. 实体字段表（Domain/Entities/InspectionModels.cs）

| 实体 | C# 属性 | C# 类型 | 数据库列 | 类型/长度 | 默认值 / 约束 |
|---|---|---|---|---|---|
| Plan | Id | long | Id | BIGINT | 主键，AUTO_INCREMENT |
| Plan | Owner | string | Owner | VARCHAR(50) | `""`；创建账号，事件不可覆盖 |
| Plan | DeviceCode | string | DeviceCode | VARCHAR(50) | `""` |
| Plan | PointCode | string | PointCode | VARCHAR(50) | `""` |
| Plan | Question | string | Question | VARCHAR(1000) | `""` |
| Plan | IntervalSeconds | int | IntervalSeconds | INT | 60–86400 |
| Plan | Enabled | bool | Enabled | BOOLEAN | `true` |
| Plan | NextAt | DateTime | NextAt | DATETIME(6) | `DateTime.UtcNow` |
| Run | Id | string | Id | CHAR(32) | `Guid.NewGuid().ToString("N")`；主键 |
| Run | PlanId | long | PlanId | BIGINT | — |
| Run | Owner | string | Owner | VARCHAR(50) | `""` |
| Run | DeviceCode | string | DeviceCode | VARCHAR(50) | `""` |
| Run | PointCode | string | PointCode | VARCHAR(50) | `""` |
| Run | Question | string | Question | VARCHAR(1000) | `""` |
| Run | MergeKey | string | MergeKey | VARCHAR(120) | `""`；UNIQUE |
| Run | Source | string | Source | VARCHAR(20) | `""`；Manual/Scheduled/Event |
| Run | State | string | State | VARCHAR(30) | `"Queued"` |
| Run | Attempts | int | Attempts | INT | `0` |
| Run | EvidenceJson | string | EvidenceJson | LONGTEXT | `""`；首次执行冻结 |
| Run | Answer | string | Answer | LONGTEXT | `""` |
| Run | Error | string | Error | VARCHAR(200) | `""` |
| Run | CreatedAt | DateTime | CreatedAt | DATETIME(6) | `DateTime.UtcNow` |
| Run | NextAt | DateTime | NextAt | DATETIME(6) | `DateTime.UtcNow` |
| Run | LeaseUntil | DateTime? | LeaseUntil | DATETIME(6) NULL | `null` |
| Run | CompletedAt | DateTime? | CompletedAt | DATETIME(6) NULL | `null` |

> `InspectionPlan` = `[Table("ai_inspection_plans")]`，`InspectionRun` = `[Table("ai_inspection_runs")]`，均 `sealed class`。冲突：文档 DDL 为 `PointCode VARCHAR(50)`，项目既有约定 `point_code` 长度 80，落库前需确认。
##### 4. 仓储 / 服务方法签名（C#）
`InspectionRepository`（`IotGateway.Infrastructure/AI/Inspections/InspectionRepository.cs`，只管理计划和队列）：
```csharp
Task<InspectionPlan?> PlanAsync(long id, CancellationToken ct);
Task<List<InspectionPlan>> DueAsync(CancellationToken ct);              // Enabled && NextAt<=UtcNow, OrderBy NextAt, Take(20)
Task AdvanceAsync(InspectionPlan plan, CancellationToken ct);           // NextAt=UtcNow+IntervalSeconds，只补当前窗口
Task<InspectionRun?> ClaimAsync(CancellationToken ct);                  // 过期 Running→Unknown；取一条 Queued/Retry；条件 UPDATE 抢占
Task EvidenceAsync(string id, string evidence, CancellationToken ct);   // 冻结 EvidenceJson
Task FinishAsync(InspectionRun row, CancellationToken ct);              // Where Id && State=="Running"；Retry 时 CompletedAt=null
Task<InspectionPlan> AddPlanAsync(InspectionPlan plan, CancellationToken ct);
Task<List<InspectionPlan>> PlansAsync(string owner, CancellationToken ct); // Owner 过滤，Id desc，Take(100)
Task<InspectionRun> EnqueueAsync(InspectionRun run, CancellationToken ct); // MergeKey 命中返回旧任务；1062 冲突 Detach 后重查
Task<bool> EnableAsync(long id, string owner, bool enabled, CancellationToken ct); // 条件更新，影响行数==1
Task<List<InspectionRun>> RunsAsync(string owner, CancellationToken ct);   // Owner 过滤，CreatedAt desc，Take(100)
Task<InspectionRun?> RunAsync(string id, string owner, CancellationToken ct); // Id + Owner
```
`InspectionService`（`IotGateway.WebAPI/AI/Inspections/InspectionService.cs`）：
```csharp
Task TickAsync(CancellationToken ct);
Task<InspectionPlan> CreateAsync(SaveInspection r, string actor, CancellationToken ct);
Task<List<InspectionPlan>> PlansAsync(string actor, CancellationToken ct);
Task<InspectionRun> SubmitAsync(long id, string actor, string source, DateTimeOffset at, CancellationToken ct);
Task<bool> EnableAsync(long id, string actor, bool value, CancellationToken ct); Task<InspectionRun> EventAsync(InspectionEvent r, CancellationToken ct);
Task<List<InspectionRun>> RunsAsync(string actor, CancellationToken ct); Task<InspectionRun?> RunAsync(string id, string actor, CancellationToken ct);
```
请求记录：
```csharp
public sealed record SaveInspection([property: Required, StringLength(50)] string DeviceCode, [property: Required, StringLength(50)] string PointCode, [property: Required, StringLength(1000)] string Question, [property: Range(60, 86400)] int IntervalSeconds);
public sealed record InspectionEvent(long PlanId, DateTimeOffset OccurredAt);
```
##### 5. 业务规则

- 三入口：手动 = 当前 JWT 用户且必须是计划所有者；定时 = 已保存计划的 `Owner`；设备事件 = 受认证事件入口，执行身份仍取计划 `Owner`，设备不能指定他人计划或 Owner。
- 创建时检查设备工具权限，执行时再检查一次，防止撤权后旧计划继续读取。
- 三项工具授权：`get_device`、`get_history`、`get_alarms`；账号不存在 → `AppException("计划所有者已停用", 403)`；缺任一项 → `AppException("巡检需要设备资料、历史、告警三项授权", 403)`。
- 创建：`RequireAsync` 后校验 `DeviceExistsAsync`，不存在 → `AppException("设备不可用", 400)`；周期 60–86400 秒。
- MergeKey：`$"plan/{plan.Id}/window/{bucket}"`，`bucket = at.ToUnixTimeSeconds() / plan.IntervalSeconds`；合并窗口 = `IntervalSeconds`（最小 60 秒）；手动、定时、事件共用。
- 窗口合并 ≠ 采样窗口：执行时读取 Day47 最近一分钟历史，证据保存实际 `windowEnd`；排队延迟必须在页面显示。
- 幂等：同提交键重发返回已有任务；`EnqueueAsync` 先按 MergeKey 查，未命中插入；MySQL `1062` 冲突时 `db.Entry(run).State = EntityState.Detached` 后按 MergeKey 重查并返回旧任务。
- 限流：单 WebAPI 实例 + 单串行 Worker，每 30 秒至多开始一项；多实例需 Quartz 共享持久化 + 统一额度治理。
- 重试：最多三次，间隔 30、60 秒（`NextAt = UtcNow.AddSeconds(30 * Attempts)`）；达到上限 `Failed`；复用第一次保存的 `EvidenceJson`，要分析最新数据应创建新窗口任务。
- 主执行超时 150 秒；落库用独立 5 秒 CTS，主超时后仍写回终态。
- `LeaseUntil = UtcNow.AddMinutes(4)`；过期 `Running` → `Unknown`，`Error = "执行租约过期，请核对后重新提交新窗口"`，不自动再次消费模型额度。
- 状态机：`Queued` 已入库未调模型；`Running` 抢占成功；`Retry` 失败待重试；`Succeeded` 分析已保存（不代表设备正常）；`Failed` 达上限；`Canceled` 计划停用/授权失效；`Unknown` Worker 失联、结果不能确认。
- 停用阻止新调度；排队任务执行前检查 `Enabled`；停用 → `Canceled` / `"计划停用"`；授权不可用 → `Canceled` / `"设备或授权不可用，请核对"`。
- 其他异常（含模型超时/服务不可用）→ `State = Attempts < 3 ? "Retry" : "Failed"`，`NextAt = UtcNow.AddSeconds(30 * Attempts)`，`Error = "分析未完成；已保留证据，按策略有限重试"`。
- 事件时间：`OccurredAt > UtcNow.AddSeconds(30)` 或 `< UtcNow.AddMinutes(-10)` → `AppException("事件不在实时分析接收窗口", 400)`；旧消息超过十分钟拒绝实时分析。
- 桥接顺序：验证来源及设备与计划映射 → `EventAsync` 入队 → 持久化成功才 Ack；失败 Nack/死信；不得在 RabbitMQ 回调中等待 DeepSeek。
- 计划保存配置，任务复制提交时配置；后续改计划不影响旧任务；`AdvanceAsync` 重启后只补当前窗口。
- 计划不存在 → 404；不属于当前用户或未启用 → 403；`EnableAsync` 更新失败 → `AppException("计划不存在或不属于当前用户", 404)`。
- 列表按 Owner 过滤并 `Take(100)`；`NextAt` 是调度时间，不是最近采样时间。
- HTTP 提交只返回 `Queued`；WPF 取消等待不删后台任务；WPF 超时应查列表 State，而不是重按“立即巡检”（MergeKey 幂等返回同一任务）；`Succeeded` 是生成完成，不是设备正常；`Unknown` 不能当成功或自动重新计费。
##### 6. 调度实现

**Quartz（文档方案）**：`InspectionJob` 加 `[DisallowConcurrentExecution]`，固定 `JobKey("ai-inspection", "ai")`，不要每次触发新建随机 JobKey；Quartz 提供独立 DI 作用域，EF 服务保持 Scoped。
```csharp
[DisallowConcurrentExecution]
public sealed class InspectionJob(InspectionService service) : IJob
{
    public Task Execute(IJobExecutionContext context) => service.TickAsync(context.CancellationToken);
}
```
合并到 Day33 已有 `AddQuartz(q => { ... })` 委托，不启动第二个 Scheduler：
```csharp
var inspectionKey = new Quartz.JobKey("ai-inspection", "ai");
q.AddJob<InspectionJob>(o => o.WithIdentity(inspectionKey));
q.AddTrigger(o => o.ForJob(inspectionKey).WithIdentity("ai-inspection-trigger", "ai")
    .WithSimpleSchedule(s => s.WithIntervalInSeconds(30).RepeatForever()
        .WithMisfireHandlingInstructionNextWithRemainingCount()));
```
**不引入 Quartz 的最小替代**：`BackgroundService` + `PeriodicTimer`，扫描间隔 30 秒；每轮创建 DI 作用域解析 Scoped `InspectionService` 调 `TickAsync`；`TickAsync` 每轮最多 `ClaimAsync` 一个任务，天然满足每 30 秒至多开始一项；异常必须在本轮捕获。
```csharp
public sealed class InspectionWorker(IServiceScopeFactory scopes, ILogger<InspectionWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(ct))
        {
            using var scope = scopes.CreateScope();
            try { await scope.ServiceProvider.GetRequiredService<InspectionService>().TickAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { log.LogError(ex, "inspection tick failed"); }
        }
    }
}
```
注册：`builder.Services.AddHostedService<InspectionWorker>();`

**SQL 要点**（依次：扫描到期计划 → 抢占 → 租约回收；抢占影响行数必须为 1，否则放弃）：
```sql
-- DueAsync：到期计划
SELECT * FROM ai_inspection_plans WHERE Enabled = 1 AND NextAt <= UTC_TIMESTAMP(6) ORDER BY NextAt LIMIT 20;
-- ClaimAsync：条件抢占
UPDATE ai_inspection_runs SET State='Running', Attempts=Attempts+1, LeaseUntil=UTC_TIMESTAMP(6) + INTERVAL 4 MINUTE
WHERE Id=@id AND State IN ('Queued','Retry');
-- 每轮抢占前：租约回收
UPDATE ai_inspection_runs SET State='Unknown', Error='执行租约过期，请核对后重新提交新窗口'
WHERE State='Running' AND LeaseUntil < UTC_TIMESTAMP(6);
```
##### 7. DI 注册与配置项

```csharp
builder.Services.AddScoped<InspectionRepository>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddAuthorization(o => o.AddPolicy("ai:event:ingest",
    p => p.RequireClaim("permission", "ai:event:ingest")));
// AppDbContext.OnModelCreating 登记 InspectionPlan、InspectionRun
// Quartz：AddJob/AddTrigger；BackgroundService 替代：AddHostedService<InspectionWorker>()
// WPF App.xaml.cs：
containerRegistry.Register<IotGateway.WPF.Services.InspectionApiClient>();
containerRegistry.RegisterForNavigation<IotGateway.WPF.Views.InspectionView,
    IotGateway.WPF.ViewModels.InspectionViewModel>();
```
控制器类级：`[ApiController, Route("api/ai/inspections"), Authorize(Policy = "user:manage")]`；`Actor => User.Identity?.Name ?? throw new UnauthorizedAccessException()`；事件 Action 额外 `[Authorize(Policy = "ai:event:ingest")]`。

| 配置项 | 文档取值 | 使用位置 |
|---|---|---|
| 调度扫描间隔 | 30 秒 | Quartz trigger / PeriodicTimer |
| DueAsync 批量 | Take(20) | 到期计划 |
| 主执行超时 | 150 秒 | `TickAsync` 链接 CTS |
| 落库超时 | 5 秒 | `FinishAsync` 独立 CTS |
| LeaseUntil | 4 分钟 | `ClaimAsync` |
| 重试上限 | 3 次 | `Attempts < 3` |
| 事件时间窗 | 超前 30 秒 / 落后 10 分钟 | `EventAsync` |
| 列表上限 | 100 | `PlansAsync` / `RunsAsync` |

> 文档未定义 `appsettings` 配置节，以上为文中硬编码数值；实现建议收敛为 `InspectionOptions`，默认值保持不变。
**对接既有代码**：`AppException`、`ApiResponse<T>`、`actor` 只取 JWT；`point_code` 长度按项目 80 或文档 50 的统一结论；模型调用可将 `IAiTextClient.CompleteAsync(...)` 包装为 Day45 `AiTaskService.CreateAsync(StartTask, actor, ct)`；同进程复用 Day47 `McpAccessService`，拆独立 Worker 用 Day47 `McpClient`。
