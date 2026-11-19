using System;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.AI
{
    // Application 使用的简化模型结果，隔离具体 MAF 返回类型。
    public sealed record AiTextResult(
        string Text,
        long? InputTokens,
        long? OutputTokens);

    // 模型调用契约：实现在 Infrastructure/AI/DeepSeekMafClient.cs。
    public interface IAiTextClient
    {
        // 服务端配置的模型标识；失败记录也要能写入 model_id，不能只靠成功结果返回。
        string ModelId { get; }

        // system：应用约束；input：本次数据；ct：调用方提供的取消信号。
        Task<AiTextResult> CompleteAsync(
            string system,
            string input,
            CancellationToken ct);
    }

    // 只携带可公开的错误分类，不向客户端泄露厂商原始响应或凭据。
    public sealed class AiProviderException : Exception
    {
        public AiProviderException(string code, string message) : base(message)
        {
            Code = code;
        }

        // 保留稳定错误码，Service 据此记录失败类型。
        public string Code { get; }
    }
}
