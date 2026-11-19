# Day14 阶段五：WPF MQTT 设备监控页

## 目标

- WPF 不安装 MQTTnet，只通过 HTTP 调用 WebAPI。
- 显示设备遥测与在线状态。
- 下发空调命令并展示 command-result。
- 保持 Prism MVVM、权限菜单、统一 ApiClient 的既有结构。

## 检查项

- [x] WPF 侧 MQTT 请求/响应模型
- [x] `IMqttDiagnosticsApiClient` 与实现
- [x] `MqttMonitorViewModel` 遥测监控、在线判断、命令下发
- [x] `MqttMonitorView.xaml` 页面布局与状态绑定
- [x] Prism DI 注册、导航注册、反射菜单
- [x] 修复“先 receive-once 再 publish”的订阅竞态
- [x] 新增后端 `publish-and-receive` 合并接口
- [x] WPF 与 API 构建通过
- [x] 真实 EMQX + 模拟器 + UI 联调

## 验证结果

- WPF 构建：0 错误，新 MQTT 文件 0 警告。
- API 构建：0 错误。
- UI 页面：在线状态正常，遥测数据正常刷新。
- 命令回执：`success=true`、`code=OK`、`message=设定温度已修改为 24.0℃`。
- 截图：`C:\Users\Public\wpf-phase5.png`
- 新接口：`POST /api/mqtt-diagnostics/publish-and-receive`

---

# Day16 阶段四：实时数据监控客户端

## 目标

- 在 WPF 中消费 `GET /api/device-data/latest`。
- 每 2 秒轮询一次，展示当前值、质量、来源、采样时间四张卡片。
- 维护最近 60 个点的实时趋势曲线，模拟器上报后页面自动刷新。
- 保持 Prism MVVM、权限菜单、统一 `ApiClient` 的既有结构。

## 检查项

- [x] `DeviceDataSnapshot` 响应模型与显示计算属性
- [x] `IDeviceDataApiClient` / `DeviceDataApiClient`（404 转为 null）
- [x] `DeviceDataMonitorViewModel`：设备/点位级联、2 秒轮询、去重、60 点窗口
- [x] `TrendChart` 自绘趋势图（不引入第三方图表库）
- [x] `DeviceDataMonitorView.xaml` 页面布局与状态绑定
- [x] Prism DI 注册、导航注册、反射菜单（排序 26）
- [x] 离开页面停止定时器并取消在途请求
- [x] 修复后端 `AddAsync` 未 await、时间字段丢失 UTC 标记等问题
- [x] WPF 与 API 构建通过
- [x] UI 自动化联调：登录 → 实时监控 → 模拟上报 → 卡片/曲线刷新

## 验证结果

- WPF 构建：0 错误。
- API 构建：0 错误（仅剩既有的重复 using 警告）。
- ViewModel 冒烟：5 秒内采集 3 个轮询点，`TrendValues=71,72,73`，离开页面后 `IsMonitoring=False`。
- UI 联调：页面显示 `已缓存 4/60 个采样点`，当前值 83、质量“良好”、来源“模拟器”、时间已转本地时区。
- 默认点位按后端配置顺序选择，不按字母序重排。

---

# Day44 复习标记：从第四课开始

## 当前状态

- 44.4–44.9 后端与 WPF 代码已全部落盘，API/WPF 均编译通过（0 错误）。
- 尚未做运行验证：SQL 未在 DataGrip 执行，`AiModel:ApiKey` 未配置，未真实调用 DeepSeek。

## 下次复习顺序

- [ ] 第四课：`44.4` 基础 → 仓储拆解需求 → 剩余内容
- [ ] 第五课：模型连接检查
- [ ] 第六课：创建分析（幂等、质量门、证据快照、两次落库）
- [ ] 第七课：列表与详情（游标分页、归属过滤）
- [ ] 第八课：WPF 页面与主菜单
- [ ] 运行验证：DataGrip 执行 `sql/44-ai-analysis.sql`
- [ ] 运行验证：User Secrets 配置 `AiModel:ApiKey`，启动 API/WPF，测连接检查/创建/分页/详情/跨账号 404

## 备注

- 用户 2026-11-05 状态不佳，主动要求先标记、下次从第四课重讲；不要直接续第五课。

---

# Day45 代码落盘：设备调查任务与工具调用

## 目标

- 让模型在授权范围内调用三个只读工具：GetDevice / GetHistory / GetAlarms。
- 任务记录保存真实工具执行过程 `calls_json`，不把模型说“查过了”当作证据。
- 新建设备调查、查询我的任务、查看调查结果与工具执行记录三个功能接入 WPF 设备助手。

## 硬前置

- [x] `sql/45-ai-tool-tasks.sql` 已在 2026-11-06 执行：`ai_tool_tasks`、最小 `alarms` 表与 PLC-001 两条种子数据已落库。
- [x] `point_code` 按项目现状统一为 80，不按文档的 50。
- [ ] 先跑 45.2 的 MAF 工具控制台样例，确认 `AIFunctionFactory` / `AsAIAgent` / DeepSeek 工具模式可用。
- [ ] 准备 PLC-001 最近一分钟 temperature 采样。

## 分层落点（避免 Infrastructure → Application 循环）

- [x] Domain：`AiToolTask`、`IAiTaskRepository`、`IAiDeviceQuery`、`IAiTaskRunner`、`DeviceQueryTools`、`ToolExecution`
- [x] Application：`AiTaskService`、`AiDeviceQueryService`（复用 `IDeviceService` + `AppDbContext`）、`StartTask`
- [x] Infrastructure：`AiTaskRepository`、`MafTaskRunner`、`DeepSeekToolModeHandler`
- [x] WebAPI：`AiTasksController`（Create/List/Get）
- [x] WPF：`AiTaskModels`、`AiTaskApiClient`、`AiTaskViewModel`、`AiTaskView`、App 注册、反射菜单

## 实施顺序

- [ ] 45.2 概念与控制台样例（明天学习时补）
- [x] SQL + 实体 + 仓储契约 + AppDbContext
- [x] 查询服务 + 三个工具 + MAF Runner
- [x] Service + Controller 三个接口
- [x] WPF 页面与菜单
- [x] 构建通过
- [x] 运行联调验证（首轮：创建 / 列表 / 详情 + `callsJson` 通过；其余清单待续）

## 验证清单

- [ ] 同一 requestKey 重发返回同一任务 ID
- [ ] 同一 requestKey 改问题返回 409
- [ ] 两个账号互相查不到对方任务，详情返回 404
- [ ] `callsJson` 能看到 Started/Succeeded/Failed
- [ ] 工具最多 4 次，超时/取消能落库
- [ ] 空告警列表不被解释为“设备安全”
- [x] API 构建 0 错误（5 个既有警告）
- [x] WPF 构建 0 错误（156 个警告；新增 Day45 文件未出现在警告列表）

## 落盘结果（2026-11-05）

- 后端：Domain / Application / Infrastructure / WebAPI 全链路代码已写入，`Program.cs` 已注册四个服务。
- SQL：`ai_tool_tasks`、最小 `alarms` 表与 PLC-001 种子数据已写入脚本，尚未在数据库执行。
- WPF：设备助手页面、ApiClient、ViewModel、反射菜单和 Prism 导航已接入，编译通过。
- 未完成：DataGrip 执行 SQL、User Secrets 配置 `AiModel:ApiKey`、真实 DeepSeek 调用与全部运行验证。
- 文档：Day45 分阶段笔记已追加到 `docs-v8/Day45-设备查询工具与分析任务编排.md` 的 `45.10`，七个阶段均补充“代码逐行讲解”。

## 备注

- 第一遍按文档跑通；Token/耗时、Prompt 版本、错误码等中台增强放第二遍。
- `alarms` 表和 PLC-001 采样是运行前置，代码编译通过不代表 `GetAlarms` / `GetHistory` 已能返回真实数据。

---

# Day44 运行修复：新建分析返回 InsufficientData（2026-11-06）

## 现象

- `新建分析` 返回 `Status=InsufficientData`、`ModelAttempted=false`、`sampleCount=0`。
- 分析 #1、#2 的窗口内均无样本；实时监控页面却有数据。

## 根因

- `device_data` 中 `PLC-001 / temperature` 最新样本停在 `2026-11-06 03:30:00Z`，
  距当前 UTC 已超过 1 分钟窗口，`ReadWindowAsync` 查不到任何行。
- 实时监控调用 `GET /api/device-data/latest`，只取最新一条、不判断时间；
  AI 分析按 `occurred_at` 过滤最近 N 分钟，两者口径不同。
- 数据库 UTC 与机器 UTC 一致（06:23:24 vs 06:23:25），时区配置无问题；
  点位配置 `temperature / ℃ / enabled=1`、设备 `status=1` 均正常。

## 处理

- 通过正式接口 `POST /api/device-data/report` 上报 3 条
  `PLC-001 / temperature / Good / ℃` 样本（id 20–22，`occurred_at` 为当前 UTC）。
- 立即用新 UUID 调用 `POST /api/ai/analyses`，不点“重发当前请求”。

## 验证结果

- 分析 #3：`Status=Succeeded`、`ModelAttempted=true`、`sampleCount=3`、`goodCount=3`、
  `elapsed=8594ms`，`answer` 838 字符，已落库。
- 输入窗口：`2026-11-06T06:24:02.853Z ~ 06:25:02.853Z`，样本 83.1 / 83.4 / 83.7 ℃。

## 复现要点

- 质量闸门要求：窗口内至少 2 条 Good 样本、单位与点位配置一致、最新样本距窗口终点 ≤60 秒。
- 手工造数必须在点击“新建分析”前 60 秒内完成，且 `occurred_at` 必须是当前 UTC；
  用 `UTC_TIMESTAMP(3)` 或走 report 接口，不要写死历史时间。
- 已生成的 `InsufficientData` 记录用同一 UUID 重发只会返回旧记录，必须“新建分析”。

## 遗留

- 目前没有持续写入 `device_data` 的模拟器；每次联调前都要重新造数。
- 如需长期联调，可给 `DeviceSimulator` 增加 HTTP 上报模式（登录后定时调用 report 接口）。

---

# Day45 运行修复：新建调查返回 500（2026-11-06）

## 现象

- WPF 设备助手页点击“新建调查”，界面显示“请求未完成：服务器内部错误”。
- API 日志 `16:59:06` 记录 `POST /api/ai/tasks` 返回 500。

## 根因

1. `StartTask` 是位置记录（positional record），但校验特性写成了 `[property: ...]`。
   ASP.NET Core MVC 在模型校验阶段抛出
   `InvalidOperationException: Record type ... has validation metadata defined on property 'Question' ...`，
   请求尚未进入 Controller，与数据库、模型调用无关。
2. `sql/45-ai-tool-tasks.sql` 尚未执行，`ai_tool_tasks` 和 `alarms` 两张表都不存在；
   即使修好模型绑定，创建任务也会因缺表失败。

## 处理

- `IotGatewayLearning.Application/AI/Tasks/AiTaskModels.cs`：
  把 `[property: Required, StringLength(...)]` 全部改为 `[param: ...]`，
  让校验元数据挂在记录主构造参数上。
- 执行 `sql/45-ai-tool-tasks.sql`：
  - 创建 `ai_tool_tasks`，`point_code VARCHAR(80)`；
  - 创建 `alarms`；
  - 写入 PLC-001 两条告警（Active / Acknowledged）。
- 重新构建 API，用正式上报接口写入 3 条新鲜样本后发起调查。

## 验证结果

- `POST /api/ai/tasks`：HTTP 200，任务 #1 `Succeeded`，回答 783 字符。
- `GET /api/ai/tasks/1`：HTTP 200，`callsJson` 长度 5567，共 3 次工具调用。
- `GET /api/ai/tasks`：HTTP 200。
- 数据库 `ai_tool_tasks`：`actor=admin`、`status=Succeeded`、`answer` 非空，
  `calls_json` 含 GetDevice / GetHistory / GetAlarms 的真实返回。
- 窗口内新鲜样本 3 条（id 23–25，86.2 / 86.6 / 87.0 ℃）。

## 复现要点

- 位置记录加 DataAnnotations，必须写 `[param: ...]`；写 `[property: ...]` 会在
  MVC 模型校验阶段直接 500，Controller 和业务层都进不去。
- 先看 API 日志的第一个异常，不要只凭“新建失败”猜数据库缺表；
  本次两个问题同时存在，模型绑定异常在前，缺表问题被遮住。
- `sql/45-ai-tool-tasks.sql` 可重复执行：建表用 `IF NOT EXISTS`，
  告警用 `WHERE NOT EXISTS` 防重。

## 遗留

- Day45 验证清单中“同 requestKey 重发 / 改问题 409 / 跨账号 404 / 第 5 次工具调用失败 /
  空告警不判安全 / truncated / 取消超时”尚未逐项验证。
- 当前没有持续写入 `device_data` 的模拟器，手工点击前仍需先造新鲜样本。

---

# Day46～50 代码落盘（2026-11-07）

## 目标

- 把 `docs-v8` 中 Day46～50 的后端与 SQL 落盘，保证 API 编译通过。
- 本批只做 Domain / Application / Infrastructure / WebAPI，WPF 页面留第二批。

## 架构假设（不引入新中间件）

| 文档要求 | 本批实现 | 原因 |
| --- | --- | --- |
| Qdrant 向量检索 | MySQL 关键词检索 + 引用落库 | 不新增容器，保留“检索 + 出处”语义 |
| Embedding 适配器 | `IKnowledgeIndex` 接口 + 关键词实现 | 接口保留，将来可换向量库 |
| MCP 服务端包 | 进程内工具目录 + HTTP 管理接口 | 保留授权、调用审计、白名单 |
| Quartz | `BackgroundService` + 轮询 | 到期扫描、租约、重试逻辑不变 |
| 钉钉 webhook | 可配置 webhook + Outbox | 默认只落 Outbox，不真发 |
| 真实设备控制 | 只做审批与审计 | 工业安全边界，AI 不直接控设备 |

## 检查项

- [x] Day46 知识库：SQL + 3 实体 + 仓储 + Service + Controller + DI
- [x] Day47 工具授权：SQL + 3 实体 + 仓储 + Service + Controller + DI
- [x] Day48 巡检计划：SQL + 2 实体 + 仓储 + Service + Controller + 后台 Worker
- [x] Day49 告警研判：SQL + 2 实体 + 仓储 + Service + Controller + Outbox Worker
- [x] Day50 控制审批：SQL + 2 实体 + 仓储 + Service + Controller + DI
- [x] `AppDbContext` 注册 12 个 DbSet 与唯一索引
- [x] `Program.cs` 注册服务与 HostedService
- [x] `dotnet build` 通过（API + WPF）

## 落盘结果（2026-11-07）

编译：`IotGatewayLearningApi` 0 错误 5 警告，`WpfIotLearningPrism` 0 错误 78 警告；
警告数与改动前一致，均为既有代码警告。

### 各天文件清单

| 天 | SQL | 实体 | 契约 / 仓储 | 服务 | 入口 |
| --- | --- | --- | --- | --- | --- |
| Day46 | `sql/46-knowledge.sql` | KnowledgeDocument / Chunk / Answer | `IKnowledgeRepository`、`KnowledgeContracts`、`KnowledgeTerms`、`KnowledgeOptions` | `KnowledgeService` | `KnowledgeController` |
| Day47 | `sql/47-mcp.sql` | McpGrant / McpCall / McpGrantChange | `IMcpRepository`、`McpContracts` | `McpAccessService` | `McpAdminController` |
| Day48 | `sql/48-inspections.sql` | InspectionPlan / InspectionRun | `IInspectionRepository`、`InspectionModels` | `InspectionService` | `InspectionsController` + `InspectionWorker` |
| Day49 | `sql/49-advisories.sql` | AiAdvisory / AiNotification | `IAdvisoryRepository`、`AdvisoryModels`、`NotificationContracts` | `AdvisoryService` | `AdvisoriesController` + `NotificationWorker` |
| Day50 | `sql/50-control-requests.sql` | ControlRequest / ControlAudit | `IControlRepository`、`ControlContracts` | `ControlRequestService` | `ControlRequestsController` |

### 与文档的落地差异（实现时确认）

1. **Day46 接口名**：实际为 `IKnowledgeVectors`，不是上文表格里的 `IKnowledgeIndex`。
2. **Day47 不引入 MCP 包**：按规格 6.4 的最小替代，`Catalog` 是进程内静态白名单，
   唯一执行入口是 `McpAccessService.InvokeAsync`，没有暴露 `/mcp`。
3. **Day47 列名**：按项目既有风格全部改 snake_case（`device_code` / `granted_by` 等），
   DDL 与实体整体一致。
4. **Day48 设备校验顺序**：先查设备存在再查三项授权。
   反过来的话，设备不存在会被“缺授权”掩盖成 403，`"设备不可用"` 的 400 永远走不到。
5. **Day48 停用/授权失效**：走 `Canceled`，不消耗重试次数。
6. **Day49 维修记录**：本项目没有维修记录表，`EvidenceAsync` 返回空数组
   并置 `knowledgeMissing`，不编造维修历史。
7. **Day49 通知实现**：通用 webhook（`Notify:*`），默认 `Enabled=false`，
   只落 Outbox 不发请求；发送器放在 WebAPI 层，避免给 Infrastructure 增加
   `Microsoft.Extensions.Http` 依赖。
8. **Day50 设备存在性**：用 EF 查询（Device 已有全局过滤器 `is_deleted=0`），
   与文档的 `COUNT(*)` 语义一致。
9. **Day50 列表权限**：`review=true` 的 403 判断放在 Service，
   由 Controller 传入 `canApprove`（取 JWT 的 `permission` Claim）。

### 尚未做（2026-11-07 已全部补完，见下一节）

- [x] WPF 页面与 `ApiClient`（第二批）。
- [x] SQL 已在 MySQL 执行，接口已完成联调。
- [x] Day48/49 的后台 Worker 已实际运行。

## 验证方式

```text
每完成一天：dotnet build IotGatewayLearningApi
全部完成：dotnet build 整个解决方案
SQL：按 46～50 顺序在 DataGrip 执行
```

---

# Day46～50 运行验证与 WPF 联调（2026-11-07）

## 环境

- API：`https://localhost:7248`，连远程 MySQL `iot_gateway`。
- WPF：`WpfIotLearningPrism`，由 UI Automation 驱动真实点击。
- 登录账号：`admin`；为审批链路新建 `reviewer`。
- 测试角色：`controller-reviewer`（id 19），权限 `device:control`、`device:approve`、`user:manage`、`ai:event:ingest`。

## Day46 知识库

- 在 API 输出目录放置 `knowledge/1001.txt`（UTF-8，S7-1200 冷却水系统手册）。
- `POST /api/ai/knowledge/import`：文档 id=1，`state=Ready`。
- `POST /api/ai/knowledge/ask`：`state=Succeeded`，回答按手册给出 4 步高温处理，
  引用块 ID `a93299fb-b264-4300-9daf-0ac349281b74`，并主动指出资料未覆盖“复位条件”。
- `GET /api/ai/knowledge/answers`：返回 1 条回答。
- `POST /api/ai/knowledge/1/retire`：`state=Retired`；再次提问返回 `NoEvidence`，
  证明退役文档不再参与检索。

## Day47 MCP 工具

- 工具目录 3 个只读工具：`get_device` / `get_history` / `get_alarms`。
- 三项授权写入后，试调用 `get_device`、`get_history` 均 `Succeeded`，调用记录已落库。

## Day48 巡检与 Worker

- 计划 1：`PLC-001 / temperature / 300 秒`，启用状态。
- 手工执行 run `91fa9100aa1a48689889314aacf1fd52`：`Succeeded`，
  证据 4345 字符、回答 371 字符。
- 后台 Worker 周期生成 `Scheduled` 任务并完成：
  `e4f0635f2d0948a397a9f4f2dcac24a7`、`a7dc31bc89c9476cb136699db25099f1` 均 `Succeeded`，
  证明“到期扫描 → 领取 → 执行 → 终态”整条链路可跑。
- 事件入口 `POST /api/ai/inspections/events`（reviewer 账号）：
  生成 run `dde5fc9a7fd2434f9c24ee8b13783038`，`source=Event`，
  Worker 领取后执行完成，`state=Succeeded`（证据 4040 字符、回答 452 字符）。
- admin 没有 `ai:event:ingest` 时同一接口返回 403，权限隔离生效。
- 数据修复：历史计划与任务的乱码问题 `???????????` 已改为 `请检查设备温度是否正常`。

## Day49 告警研判

- 研判 `f9744479034e48d18cda45dc701eb1b4`：`Active` → `Acknowledged` → `Closed`，
  中文备注正常落库。
- 通知 Outbox 生成 1 条 `Pending`（`Notify:Enabled=false`），符合“默认只落库不真发”的设计。

## Day50 控制申请审批

| 场景 | 结果 |
| --- | --- |
| admin 提交 `AC-001 / setTemperature / 26.5` | 200，`Pending`，id `e62d26a766f44b678579b3f0d34a6e7a` |
| reviewer 查审批队列 | 能看到该申请 |
| reviewer 批准 | 200，`Approved`，`reviewer=reviewer`，`executeBefore=+5 分钟` |
| 审计链 | `Submit`（admin）+ `Approve`（reviewer）两条 |
| 同 key 同参数重发 | 同一 id，返回当前状态 `Approved` |
| 同 key 不同参数 | 409 `请求键已用于不同申请` |
| reviewer 提交后自审 | 403 `不能审批自己的申请` |
| admin 审批 reviewer 的申请 | 403（JWT 无 `device:approve`） |
| reviewer 撤销本人 `Pending` | 200，`Canceled`，审计 `Submit` + `Cancel` |
| 重复撤销 / 撤销他人申请 | 409 |
| 非白名单设备 / 温度越界 / 关机带目标 | 400，对应文案 |

## WPF 客户端

- 菜单反射生成 17 项，五个新页面全部出现（按 tooltip 确认）：
  `设备知识库(62)`、`MCP工具管理(63)`、`设备巡检(64)`、`AI告警研判(65)`、`控制申请审批(66)`。
- 五个页面均成功导航并拉到真实数据：
  - 知识库：资料 `Retired` + 回答 `Succeeded` / `NoEvidence`；
  - MCP：3 个工具目录，授权 / 变更 / 调用三个页签；
  - 巡检：计划 1 与周期、下次调度时间；
  - 研判：`Closed` 研判及完整摘要；
  - 控制审批：`Approved` 与 `Pending` 两条申请。
- 在控制审批页用界面完成一次真实 POST：
  “新幂等键”生成 `72f5d35ec445404db2103a0edd6e6505`，填写后提交，
  页面提示“申请已提交，状态 Pending”，列表新增
  `93ee2b1942594c04b8c494f80d60ad76 / AC-001 / setTemperature / 25.5 / admin / Pending`。

## 本轮修复的缺陷

1. **创建用户 500**：`UserService.CreateUserAsync` 未给 `AvatarUrl` 赋值，
   而数据库 `avatar_url` 为 `NOT NULL`。修复：初始化为 `string.Empty`。
2. **提交控制申请 500**：`ControlAudit` 与 `ControlRequest` 未配置 EF 关系，
   同一次 `SaveChanges` 里先插审计触发外键失败。修复：`AppDbContext` 增加
   `HasOne<ControlRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(Restrict)`。
3. **事件入口永远 403**：`ai:event:ingest` 只注册了 Policy，`permissions` 表没有记录。
   修复：`sql/48-inspections.sql` 增加 `INSERT IGNORE`，已执行（权限 id=9）。
4. **巡检问题乱码**：历史数据修正，非代码问题。

## 遗留

- `Notify:Enabled=false` 时通知只落 Outbox；真实 webhook 发送需配置接收端后再验证。
- 审批通过仍不下发设备命令，执行属于 Day51。
- `ai:event:ingest` 未默认分配给任何角色，桥接账号需单独授权。
