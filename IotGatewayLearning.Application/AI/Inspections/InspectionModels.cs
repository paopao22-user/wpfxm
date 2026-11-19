using System;
using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.AI.Inspections
{
    /// <summary>
    /// 创建巡检计划请求。
    /// 位置记录必须用 [param:]，用 [property:] 会在进 Controller 之前抛 500。
    /// IntervalSeconds 限制 60-86400：太短会把模型额度打光，太长等于没巡检。
    /// </summary>
    public sealed record SaveInspection(
        [param: Required, StringLength(50, MinimumLength = 1)] string DeviceCode,
        [param: Required, StringLength(80, MinimumLength = 1)] string PointCode,
        [param: Required, StringLength(1000, MinimumLength = 1)] string Question,
        [param: Range(60, 86400)] int IntervalSeconds);

    /// <summary>
    /// 设备事件。OccurredAt 由设备侧给出，服务端只接受实时窗口内的事件；
    /// 执行身份仍取计划 Owner，事件入口不能指定别人的计划。
    /// </summary>
    public sealed record InspectionEvent(long PlanId, DateTimeOffset OccurredAt);
}
