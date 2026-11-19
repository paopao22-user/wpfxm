using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Domain.AI.Mcp
{
    /// <summary>
    /// 工具目录项。目录写死在代码里，不接受数据库或用户上传扩展。
    /// </summary>
    public sealed record ToolInfo(string Name, string Description, string Arguments);

    /// <summary>
    /// 授予 / 撤销授权请求。
    /// Actor 是被授权人；操作员另从 JWT 取，两者不是同一个身份。
    /// 位置记录必须用 [param:]，写成 [property:] 会让 MVC 在进 Controller 前就抛 500。
    /// </summary>
    public sealed record McpGrantRequest(
        [param: Required, StringLength(50, MinimumLength = 1)] string Actor,
        [param: Required, StringLength(50, MinimumLength = 1)] string DeviceCode,
        [param: Required, StringLength(50, MinimumLength = 1)] string Tool);

    /// <summary>
    /// 试调用请求。PointCode 只有 get_history 用得上，其余工具忽略。
    /// 长度取 80，与项目既有 point_code 约定一致。
    /// </summary>
    public sealed record McpInvokeRequest(
        [param: Required, StringLength(50, MinimumLength = 1)] string DeviceCode,
        [param: Required, StringLength(50, MinimumLength = 1)] string Tool,
        [param: StringLength(80, MinimumLength = 1)] string PointCode = "temperature");

    /// <summary>
    /// 试调用返回。Status 只会是 Succeeded/Denied/Failed/Canceled，
    /// Started 是内部中间态，不会出现在响应里。
    /// </summary>
    public sealed record McpReadResult(string CallId, string Status, object? Data);
}
