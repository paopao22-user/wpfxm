using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.AI.Advisories
{
    /// <summary>
    /// 创建研判请求。RunId 是 Day48 巡检任务 ID（32 位无连字符 GUID）。
    /// KnowledgeAnswerId 可为空：为空时明确告诉模型"没有可用资料"，不假装有依据。
    /// </summary>
    public sealed record CreateAdvisory(
        [param: Required, StringLength(32, MinimumLength = 32)] string RunId,
        long? KnowledgeAnswerId);

    /// <summary>
    /// 确认/关闭共用同一个请求体：
    /// close=false → Active → Acknowledged；close=true → Acknowledged → Closed。
    /// </summary>
    public sealed record HandleAdvisory(
        bool Close,
        [param: Required, StringLength(500, MinimumLength = 1)] string Remark);
}
