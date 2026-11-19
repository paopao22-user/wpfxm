using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IotGatewayLearning.Domain.Repositories;

namespace IotGatewayLearning.Application.AI
{
    // POST 请求合同；特性用于 HTTP 参数验证，Service 仍重复检查业务边界。
    public sealed class CreateAiAnalysisRequest
    {
        // 一次用户意图固定一个 UUID，网络重发时不能重新生成。
        [Required]
        public string RequestId { get; set; } = "";

        // 设备和点位编码由后端核对，用户不直接上传采样值。
        [Required, StringLength(50)]
        public string DeviceCode { get; set; } = "";

        // 点位编码与设备编码组合确定本次分析对象。
        // 你项目 DeviceData/DevicePoint 的 point_code 是 80，这里同步放宽。
        [Required, StringLength(80)]
        public string PointCode { get; set; } = "";

        // 教学窗口限制为 1 至 10 分钟，避免一次传入过多历史数据。
        [Range(1, 10)]
        public int WindowMinutes { get; set; } = 1;
    }

    // record 保存一组数据，生成后不提供逐字段修改操作。
    // 发送给模型的输入快照：时间、单位、统计、样本和缺失说明。
    public sealed record AiAnalysisInput(
        string DeviceCode,
        string PointCode,
        DateTime WindowStart,
        DateTime WindowEnd,
        string? Unit,
        int SampleCount,
        int GoodCount,
        decimal? Minimum,
        decimal? Maximum,
        decimal? Average,
        List<AiSample> Samples,
        string[] Notes);

    // 连接检查只返回模型名、回答、耗时和用量，不返回服务密钥。
    public sealed record AiConnectionResult(
        string ModelId,
        string Reply,
        long ElapsedMs,
        long? InputTokens,
        long? OutputTokens);

    // 列表摘要省略大段输入与回答，详情再按 ID 单独读取。
    public sealed record AiAnalysisRow(
        long Id,
        string RequestId,
        string DeviceCode,
        string PointCode,
        string Status,
        bool ModelAttempted,
        DateTime CreatedAt);

    // Items 为当前页，NextBeforeId 为 null 时表示没有下一页。
    public sealed record AiAnalysisPage(
        List<AiAnalysisRow> Items,
        long? NextBeforeId);

    // JsonElement 保留快照结构，读取详情不重新查询历史数据。
    // 完整详情：Input 为冻结的 JSON，Answer 与 Error 字段按 Status 解释。
    public sealed record AiAnalysisDetail(
        long Id,
        string RequestId,
        string Status,
        bool ModelAttempted,
        string ModelId,
        string PromptVersion,
        JsonElement Input,
        string Answer,
        string? ErrorCode,
        string? ErrorMessage,
        long ElapsedMs,
        long? InputTokens,
        long? OutputTokens,
        DateTime CreatedAt,
        DateTime? CompletedAt);
}
