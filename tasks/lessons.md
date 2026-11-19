# Day14 教训

## 1. 两个短连接接口不能靠固定延时拼时序

- 现象：WPF 先调用 `receive-once`，再 `Task.Delay(600)` 后调用 `publish`；模拟器已经回了 command-result，前端却等待 10 秒超时。
- 根因：`receive-once` 内部要重新 CONNECT + SUBSCRIBE，固定 600ms 不能保证订阅已在 Broker 生效。回执在订阅生效前发出，就会永久丢失。
- 处理：新增 `publish-and-receive`，在同一个 MQTT 客户端内先 `SubscribeAsync`，再 `PublishAsync`，最后等待包含 `commandId` 的回执。
- 规则：有先后依赖的跨网络操作，不要用 sleep 猜时序；把依赖操作放进同一个连接或同一个服务端事务。

## 2. WPF `Run.Text` 默认 TwoWay

- 现象：实时监控页导航时报 `XamlParseException`，内层是“无法对只读属性 LatestRawValueText 进行 TwoWay 绑定”。
- 根因：`Run.Text` 的依赖属性默认 `BindsTwoWayByDefault`，而 `LatestRawValueText` 是只读计算属性。
- 处理：写成 `Text="{Binding LatestRawValueText, Mode=OneWay}"`。
- 规则：给 `Run`、`TextBox` 等默认双向绑定的目标绑只读属性时，显式写 `Mode=OneWay`。

## 3. 实时监控有数据 ≠ AI 分析有数据

- 现象：`GET /api/device-data/latest` 能看到温度，`新建分析` 却返回 `InsufficientData`、`sampleCount=0`。
- 根因：latest 只取“最新一条”，不判断它是几秒前还是几小时前；AI 分析按 `occurred_at`
  过滤最近 1~10 分钟窗口，并要求至少 2 条 Good 样本、单位一致、最新样本不超过 60 秒。
- 处理：在点击“新建分析”前 60 秒内，用 `UTC_TIMESTAMP(3)` 或 report 接口写入新鲜样本；
  已失败的记录不要用同一 UUID 重发，必须新建分析。
- 规则：时间窗口类查询要同时核对“数据时间字段、窗口基准时钟、窗口终点是否包含”；
  不要拿“最新一条”接口的结果推断窗口查询一定成功。

## 4. 位置记录的校验特性必须打在构造参数上

- 现象：`POST /api/ai/tasks` 直接 500，WPF 只显示“服务器内部错误”；日志首个异常是
  `Record type 'StartTask' has validation metadata defined on property 'Question' ...`。
- 根因：`StartTask` 是位置记录（positional record），校验特性写成了 `[property: Required, ...]`。
  ASP.NET Core MVC 在模型校验阶段发现元数据挂在属性上会被忽略，直接抛异常，
  请求还没进入 Controller。
- 处理：改为 `[param: Required, StringLength(...)]`，把校验元数据绑定到主构造参数。
- 规则：位置记录 + DataAnnotations 一律用 `[param: ...]`；
  接口 500 先看日志里的第一个异常，再判断是不是数据库/业务层问题。
