using System.ComponentModel.DataAnnotations;

namespace IotGatewayLearning.Application.AI.Knowledge
{
    // 导入请求：文件、设备、型号与版本必填。
    public sealed record ImportKnowledge(
        [param: Range(1, long.MaxValue)] long FileId,
        [param: Range(1, long.MaxValue)] long DeviceId,
        [param: Required, StringLength(80)] string ModelCode,
        [param: Required, StringLength(40)] string Revision);

    // 提问请求：型号和问题必填，问题最多 1000 字符。
    public sealed record AskKnowledge(
        [param: Required, StringLength(80)] string ModelCode,
        [param: Required, StringLength(1000)] string Question);
}
