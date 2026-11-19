using System;

namespace IotGatewayLearning.Domain.AI.Tasks
{
    // 一条记录 = 一次真实工具调用的开始/结束状态和实际返回值。
    public sealed record ToolExecution(
        string Tool,
        DateTime StartedAt,
        DateTime? CompletedAt,
        string Status,
        string Result);
}
