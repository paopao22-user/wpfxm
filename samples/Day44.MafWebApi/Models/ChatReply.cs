namespace Day44.MafWebApi.Models;

/// <summary>
/// 入门问答响应。Token 未提供时保留 null，不要填 0。
/// </summary>
public sealed record ChatReply(
    string ModelId,        // 本次配置的模型
    string Reply,          // 模型生成的最终文字
    long? InputTokens,     // 未提供时为 null
    long? OutputTokens);   // 不等同于金额
