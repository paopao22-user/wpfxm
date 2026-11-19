**Day50 实现规格：控制申请人工审批与操作审计**

来源：`docs-v8/Day50-控制申请人工审批与操作审计.md`。本章只做审批与审计，不下发设备控制。
##### 0. 文档命名到本项目落位

| 文档路径/命名 | 本项目落位 |
|---|---|
| `IotGateway.Domain.Entities` | `IotGatewayLearning.Domain.Entities`；`IotGatewayLearning.Domain/Entities/ControlModels.cs` |
| `IotGateway.Infrastructure.Control.ControlRepository` | 接口 `IotGatewayLearning.Domain/Repositories/IControlRepository.cs`；实现 `IotGatewayLearning.Infrastructure/Repositories/Implementations/ControlRepository.cs` |
| `IotGateway.WebAPI.Control.ControlRequestService` | 接口 `IotGatewayLearning.Application/Services/Interfaces/IControlRequestService.cs`；实现 `IotGatewayLearning.Application/Services/Implementations/ControlRequestService.cs` |
| `IotGateway.WebAPI.Controllers.ControlRequestsController` | `IotGatewayLearningApi/Controllers/ControlRequestsController.cs` |
| `IotGateway.WebAPI.Control.ControlContracts` | `IotGatewayLearning.Application/DTOs/Control/ControlContracts.cs` |
| `IotGateway.Common.Exceptions` / `Responses` | `IotGatewayLearning.Common.Exceptions` / `IotGatewayLearning.Common.Responses` |

本项目 `ApiResponse<T>` 无 `Success` 静态方法，统一写 `Ok(new ApiResponse<object>{ Code=200, Message="操作成功", Data=... })`；`actor` 取 `User.Identity?.Name ?? string.Empty`；新增 `PermissionCodes.DeviceApprove="device:approve"`、`PermissionCodes.ControlView="control:view"`。
##### 1. 功能清单

| 功能 | 接口 | 权限 |
|---|---|---|
| 提交控制申请 | POST /api/control-requests | device:control |
| 查询本人申请 | GET /api/control-requests | device:control |
| 查询审批队列 | GET /api/control-requests?review=true | device:approve |
| 查看申请与审计 | GET /api/control-requests/{id} | 本人或审批人 |
| 批准申请 | POST /api/control-requests/{id}/decision | device:approve |
| 驳回申请 | 同上，approve=false | device:approve |
| 撤销本人申请 | POST /api/control-requests/{id}/cancel | device:control |

**1.1 提交控制申请** `POST /api/control-requests`
```json
{
  "requestKey": "a16c017f860d429482023aecb9e7c7ff",
  "deviceCode": "AC-001",
  "action": "setTemperature",
  "target": 24,
  "reason": "室温偏高，申请降低设定值"
}
```
```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "id": "request-example",
    "state": "Pending"
  }
}
```

**1.2 查询本人控制申请** `GET /api/control-requests?review=false`，不发送 JSON 请求体。
```json
{"code":200,"message":"操作成功","data":[]}
```

**1.3 查询待审批申请** `GET /api/control-requests?review=true`，不发送 JSON 请求体。
```json
{"code":200,"message":"操作成功","data":[]}
```

**1.4 查看控制申请与操作审计** `GET /api/control-requests/request-example`，不发送 JSON 请求体。
```json
{"code":200,"message":"操作成功","data":{"request":{"id":"request-example","state":"Pending"},"audits":[]}}
```

**1.5 批准控制申请** `POST /api/control-requests/{id}/decision`
```json
{"approve":true,"remark":"已核对虚拟空调与影响，允许执行"}
```
```json
{"code":200,"message":"操作成功","data":{"id":"request-example","state":"Approved"}}
```

**1.6 驳回控制申请** `POST /api/control-requests/{id}/decision`
```json
{"approve":false,"remark":"设备正在检修，当前不允许改变状态"}
```
```json
{"code":200,"message":"操作成功","data":{"id":"request-example","state":"Rejected"}}
```

**1.7 撤销本人控制申请** `POST /api/control-requests/{id}/cancel`
```json
{"remark":"现场条件变化，撤回本次申请"}
```
```json
{"code":200,"message":"操作成功","data":{"id":"request-example","state":"Canceled"}}
```

所有功能均带 `Authorization: Bearer {{token}}`。
##### 2. SQL DDL

文件：`sql/50-control-requests.sql`，在已有 `iot_gateway` 数据库执行。
```sql
CREATE TABLE control_requests (
 Id CHAR(32) PRIMARY KEY,
 RequestKey CHAR(32) NOT NULL UNIQUE,
 DeviceCode VARCHAR(50) NOT NULL,
 Action VARCHAR(40) NOT NULL,
 Target DECIMAL(5,1) NULL,
 Reason VARCHAR(500) NOT NULL,
 Requester VARCHAR(50) NOT NULL,
 Reviewer VARCHAR(50) NOT NULL,
 State VARCHAR(30) NOT NULL,
 CreatedAt DATETIME(6) NOT NULL,
 ReviewBefore DATETIME(6) NOT NULL,
 ExecuteBefore DATETIME(6) NULL,
 UpdatedAt DATETIME(6) NOT NULL,
 INDEX ix_control_queue(State, CreatedAt)
);
CREATE TABLE control_audits (
 Id BIGINT PRIMARY KEY AUTO_INCREMENT,
 RequestId CHAR(32) NOT NULL,
 Actor VARCHAR(50) NOT NULL,
 Action VARCHAR(40) NOT NULL,
 Remark VARCHAR(500) NOT NULL,
 CreatedAt DATETIME(6) NOT NULL,
 INDEX ix_control_audit(RequestId, Id),
 CONSTRAINT fk_control_audit FOREIGN KEY(RequestId) REFERENCES control_requests(Id)
);
INSERT IGNORE INTO permissions(code,name,description,status)
VALUES ('device:approve','审批设备控制','审批他人控制申请及核对未知结果',1);
```
##### 3. 实体字段表

**3.1 ControlRequest（表 `control_requests`）**

| C# 属性 / 类型 | 数据库列 / 类型 / 长度 | 可空 / 默认值 |
|---|---|---|
| `Id` / `string` | `Id` / `CHAR(32)` | 主键；`Guid.NewGuid().ToString("N")` |
| `RequestKey` / `string` | `RequestKey` / `CHAR(32)` | NOT NULL，唯一；`""` |
| `DeviceCode` / `string` | `DeviceCode` / `VARCHAR(50)` | NOT NULL；`""` |
| `Action` / `string` | `Action` / `VARCHAR(40)` | NOT NULL；`""` |
| `Target` / `decimal?` | `Target` / `DECIMAL(5,1)` | NULL；`null` |
| `Reason` / `string` | `Reason` / `VARCHAR(500)` | NOT NULL；`""` |
| `Requester` / `string` | `Requester` / `VARCHAR(50)` | NOT NULL；`""` |
| `Reviewer` / `string` | `Reviewer` / `VARCHAR(50)` | NOT NULL；`""` |
| `State` / `string` | `State` / `VARCHAR(30)` | NOT NULL；`"Pending"` |
| `CreatedAt` / `DateTime` | `CreatedAt` / `DATETIME(6)` | NOT NULL；`DateTime.UtcNow` |
| `ReviewBefore` / `DateTime` | `ReviewBefore` / `DATETIME(6)` | NOT NULL；`DateTime.UtcNow.AddMinutes(30)` |
| `ExecuteBefore` / `DateTime?` | `ExecuteBefore` / `DATETIME(6)` | NULL；`null` |
| `UpdatedAt` / `DateTime` | `UpdatedAt` / `DATETIME(6)` | NOT NULL；`DateTime.UtcNow` |

**3.2 ControlAudit（表 `control_audits`）**

| C# 属性 / 类型 | 数据库列 / 类型 / 长度 | 可空 / 默认值 |
|---|---|---|
| `Id` / `long` | `Id` / `BIGINT` | 主键，`AUTO_INCREMENT` |
| `RequestId` / `string` | `RequestId` / `CHAR(32)` | NOT NULL；`""` |
| `Actor` / `string` | `Actor` / `VARCHAR(50)` | NOT NULL；`""` |
| `Action` / `string` | `Action` / `VARCHAR(40)` | NOT NULL；`""` |
| `Remark` / `string` | `Remark` / `VARCHAR(500)` | NOT NULL；`""` |
| `CreatedAt` / `DateTime` | `CreatedAt` / `DATETIME(6)` | NOT NULL；`DateTime.UtcNow` |

**3.3 请求合同**
```csharp
public sealed record SubmitControl([property: Required] string RequestKey, [property: Required, StringLength(50)] string DeviceCode,
    [property: Required, StringLength(40)] string Action, decimal? Target, [property: Required, StringLength(500)] string Reason);
public sealed record ReviewControl(bool Approve, [property: Required, StringLength(500)] string Remark);
public sealed record CancelControl([property: Required, StringLength(500)] string Remark);
```
##### 4. 仓储方法签名
```csharp
public Task<ControlRequest?> FindAsync(string id, CancellationToken ct);
public Task<ControlRequest?> ByKeyAsync(string key, CancellationToken ct);
public Task<List<ControlAudit>> AuditsAsync(string id, CancellationToken ct);
public Task<bool> DeviceExistsAsync(string code, CancellationToken ct);
public Task<List<ControlRequest>> ListAsync(string actor, bool review, CancellationToken ct);
public Task<ControlRequest> AddAsync(ControlRequest row, CancellationToken ct);
public Task<bool> DecideAsync(string id, string actor, bool approve, string remark, CancellationToken ct);
public Task<bool> CancelAsync(string id, string actor, string remark, CancellationToken ct);
```
```csharp
// AddAsync：同一事务 db.Add(row) + ControlAudit{RequestId=row.Id,Actor=row.Requester,Action="Submit",Remark=row.Reason}；
//   DbUpdateException 内层 MySqlException.Number==1062 时回滚、db.ChangeTracker.Clear()、返回 ByKeyAsync(row.RequestKey,ct)，
//   找不到则 throw new InvalidOperationException("请求键冲突但记录不可见")；成功 CommitAsync 后返回 row。
// DecideAsync：WHERE x.Id==id && x.State=="Pending" && x.Requester!=actor && x.ReviewBefore>now；
//   SET State=approve?"Approved":"Rejected", Reviewer=actor, UpdatedAt=now,
//       ExecuteBefore=approve?(DateTime?)now.AddMinutes(5):null；
//   changed!=1 返回 false；否则同事务 db.Add(new ControlAudit{RequestId=id,Actor=actor,Action=approve?"Approve":"Reject",Remark=remark})。
// CancelAsync：WHERE x.Id==id && x.Requester==actor && (x.State=="Pending"||x.State=="Approved")；
//   SET State="Canceled", UpdatedAt=DateTime.UtcNow；count!=1 返回 false；
//   否则同事务 db.Add(new ControlAudit{RequestId=id,Actor=actor,Action="Cancel",Remark=remark})。
// DeviceExistsAsync：SELECT COUNT(*) AS Count FROM devices WHERE device_code={code} AND is_deleted=0 AND status=1；Count==1 为 true。
// ListAsync：review ? State=="Pending" : Requester==actor；OrderByDescending(CreatedAt).Take(100)。
```
##### 5. 业务规则

- 状态取值：`Pending`、`Approved`、`Rejected`、`Canceled`；Day51 另用 `Executing`、`Applied`、`Failed`、`Unknown`、`Verified`。`Expired` 只是页面按时间显示的不可执行条件，不落库，不启动作业批量改状态。
- 合法迁移：`Pending -> Approved -> Executing -> Applied / Failed / Unknown`；`Pending -> Rejected`；`Pending / Approved -> Canceled`；`Unknown -> Verified`（人工核对，Day51）。
- `Approved` 且 `ExecuteBefore` 已过期，禁止开始执行。
- `ReviewBefore`：Pending 申请 30 分钟内允许审批。`ExecuteBefore`：批准后 5 分钟内允许开始执行，批准时写 `now.AddMinutes(5)`，驳回写 `null`。
- 有效期只限制“开始执行”；命令已发出后不允许通过撤销伪装撤回。
- `Requester` 是申请人，`Reviewer` 是审批人，均由已认证 JWT 用户名取得；客户端不能提交这两个字段。
- 不可自审：Service 检查 `row.Requester==actor` 抛 `AppException("不能审批自己的申请",403)`；数据库条件更新 WHERE 同时带 `Requester!=actor`。
- 白名单：仅 `AC-001`；动作仅 `setTemperature`、`powerOff`；其他设备抛 `AppException("设备不在控制白名单",400)`。
- `setTemperature`：`target` 不能为 null，范围 16~30，最多一位小数（`decimal.Round(target.Value,1)!=target` 拒绝），否则抛 `AppException("设定值必须为16至30℃，最多一位小数",400)`。
- `powerOff`：`target` 必须为 null；否则抛 `AppException("仅允许设温或不带目标值的关机申请",400)`。
- `remark`/`reason` 非空且 ≤500 字，保存前 `Trim()`；否则抛 `AppException("说明必填，最多500字",400)`。申请理由与审批备注分开保存。
- `requestKey` 必须为 32 位无横线 UUID（`Guid.TryParseExact(...,"N",...)`），否则抛 `AppException("请求键必须为32位UUID",400)`。
- 相同 Key + 相同参数重发返回原申请；Key 已用于不同 `Requester/DeviceCode/Action/Target/Reason` 抛 `AppException("请求键已用于不同申请",409)`。并发由数据库唯一键与事务裁决，不靠按钮禁用。
- 提交前校验设备存在（`devices.device_code=code AND is_deleted=0 AND status=1`），不可用抛 `AppException("设备不可用",400)`。
- 列表：`review=true` 只查 `State=="Pending"`，`review=false` 只查 `Requester==actor`；`OrderByDescending(CreatedAt).Take(100)`。
- `review=true` 但无 `device:approve` 抛 `AppException("没有审批队列权限",403)`。
- 详情：非本人且非审批人抛 `AppException("申请不存在",404)`；返回 `{request=row, audits=AuditsAsync(id)}`。
- 批准前重新执行 `CheckTarget` 与 `DeviceExistsAsync`；驳回不重新校验目标与设备。
- 批准/驳回条件更新 `changed!=1` 时抛 `AppException("申请已处理或超过审批有效期，请刷新",409)`。并发审批只有一个请求 `changed==1`，其余拿不到重复批准。
- 撤销条件：仅 `Requester==actor` 且状态为 `Pending` 或 `Approved`；否则抛 `AppException("申请不存在、非本人或已经开始执行",409)`。与执行并发时只有一个状态转换成功。
- 审计动作仅 `Submit`、`Approve`、`Reject`、`Cancel`；字段 `Id`、`RequestId`、`Actor`、`Action`、`Remark`、`CreatedAt`。只记录人对申请的操作，不记录设备命令前后值或执行结果。
- 状态更新与审计写入必须同一事务：先 `ExecuteUpdateAsync`，再 `db.Add(ControlAudit)`，最后 `CommitAsync`；`changed!=1` 时不写审计。
- 时间统一 UTC、`DATETIME(6)`；WPF 展示时再转本地时间。主键 `Id` 为 `Guid.NewGuid().ToString("N")`。
- 审批结果以服务器返回为准，不能因按钮变灰推断申请已批准。
##### 6. 是否需要真实执行控制

- 不需要。本章明确“申请保存成功不发送设备命令；审批通过也不发送命令”。`DecideAsync` 只改 `State`、`Reviewer`、`UpdatedAt`、`ExecuteBefore`，没有任何一行发送 MQTT、调用网关、Modbus 或设备控制接口。
- 本章不实现 `Executing / Applied / Failed / Unknown / Verified` 状态迁移；这些由 Day51 的执行功能处理。Day51 只接受已批准且仍有效的申请。
- 不把任意 MQTT Topic、PLC 地址、脚本或设备命令交给模型和 WPF；模型只能提出建议，申请参数由用户核对后提交。
- 关机也是控制请求，不是急停系统；PLC 联锁、现场急停和硬实时保护仍由现场系统承担。
##### 7. DI 注册与配置项

**7.1 EF 模型配置（追加到 `AppDbContext.OnModelCreating`）**
```csharp
modelBuilder.Entity<ControlRequest>().HasIndex(x => x.RequestKey).IsUnique();
modelBuilder.Entity<ControlRequest>().Property(x => x.Target).HasPrecision(5, 1);
modelBuilder.Entity<ControlAudit>();
```

**7.2 服务注册（`Program.cs`）**
```csharp
builder.Services.AddScoped<IControlRepository, ControlRepository>();
builder.Services.AddScoped<IControlRequestService, ControlRequestService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("device:approve", p => p.RequireClaim("permission", "device:approve"));
    // device:control 已在设备控制业务配置；不要覆盖原策略。
    options.AddPolicy("control:view", p => p.RequireAssertion(c =>
        c.User.HasClaim("permission", "device:control") ||
        c.User.HasClaim("permission", "device:approve")));
});
```

**7.3 配置项与初始化**

- 无新增 `appsettings.json` 配置项；复用现有 `AppDbContext` 连接串。
- SQL 脚本：`sql/50-control-requests.sql`，数据库 `iot_gateway`。
- 新增权限：`device:approve`，用 `INSERT IGNORE INTO permissions(...)` 写入；通过 Day06 角色权限页面分配给审批角色，不授予全部操作员。
- 已有 `device:control` 策略保持不变；`control:view` 允许 `device:control` 或 `device:approve` 任一。
- 需创建两个不同测试账号：申请人 A（`device:control`）与审批人 B（`device:approve`）。
