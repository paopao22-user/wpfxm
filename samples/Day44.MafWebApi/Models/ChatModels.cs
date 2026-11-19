using System.ComponentModel.DataAnnotations;

namespace Day44.MafWebApi.Models;

/// <summary>
/// 入门问答请求。question 来自 HTTP 请求体。
/// </summary>
public sealed class ChatRequest
{
    // Required 拒绝 null 和空白；StringLength 限制问题长度。
    [Required, StringLength(1000)]
    public string Question { get; set; } = "";
}
