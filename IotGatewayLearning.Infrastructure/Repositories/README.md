# 仓储目录约定（新老两代并存）

- `Infrastructure/Repositories/Interfaces/`：旧模块契约（Day03–Day43，历史遗留：
  Application 反向引用 Infrastructure，接口和实现都放在 Infrastructure）。
- `Domain/Repositories/`：新模块契约（Day44 起，依赖倒置：接口放 Domain，
  Infrastructure 负责实现）。
- `Infrastructure/Repositories/Implementations/`：所有仓储实现（新旧都在这，
  找实现只看这一个目录）。

新代码规则：

1. 接口 → `Domain/Repositories/IXxxRepository.cs`
2. 实现 → `Infrastructure/Repositories/Implementations/XxxRepository.cs`
3. DI 注册 → `IotGatewayLearningApi/Program.cs`
4. 旧仓储保持不动，未来按模块渐进迁移。
