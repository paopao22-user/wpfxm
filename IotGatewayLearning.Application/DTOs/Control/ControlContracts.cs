using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.DTOs.Control
{
    /// <summary>
    /// 提交控制申请。
    /// Requester 不在合同里：申请人只从 JWT 取，客户端不能自己指定。
    /// 位置记录必须用 [param:]，写成 [property:] 会在进 Controller 前抛 500。
    /// </summary>
    public sealed record SubmitControl(
        [param: Required, StringLength(32, MinimumLength = 32)] string RequestKey,
        [param: Required, StringLength(50, MinimumLength = 1)] string DeviceCode,
        [param: Required, StringLength(40, MinimumLength = 1)] string Action,
        decimal? Target,
        [param: Required, StringLength(500, MinimumLength = 1)] string Reason);

    /// <summary>
    /// 审批：approve=true 批准，false 驳回；remark 必填。
    /// </summary>
    public sealed record ReviewControl(
        bool Approve,
        [param: Required, StringLength(500, MinimumLength = 1)] string Remark);

    /// <summary>
    /// 撤销本人申请，必须写原因。
    /// </summary>
    public sealed record CancelControl(
        [param: Required, StringLength(500, MinimumLength = 1)] string Remark);
}
