using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.AI.Tasks
{
    // 新建调查的 HTTP 合同；point_code 按项目现状使用 80。
    public sealed record StartTask(
        [param: Required, StringLength(100)] string RequestKey,
        [param: Required, StringLength(50)] string DeviceCode,
        [param: Required, StringLength(80)] string PointCode,
        [param: Required, StringLength(1000)] string Question);
}
