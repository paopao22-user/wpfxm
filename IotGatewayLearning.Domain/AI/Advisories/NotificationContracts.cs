using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.AI.Advisories
{
    /// <summary>
    /// 通知发送契约。实现放在 Infrastructure，Application 只依赖这个接口。
    /// 发送失败必须抛异常，不能静默返回——否则 Outbox 会把"没发出去"记成"已投递"。
    /// </summary>
    public interface INotificationSender
    {
        Task SendAsync(AiAdvisory advisory, CancellationToken ct);
    }

    /// <summary>
    /// 通知配置。
    /// 默认 Enabled=false：只落 Outbox 待办、不发真实请求，
    /// 这样本地学习和演示不会误发给真实群。
    /// </summary>
    public sealed class NotificationOptions
    {
        public const string Section = "Notify";

        public bool Enabled { get; set; }

        // 完整 HTTPS 地址，主机必须在 AllowedHosts 里。
        public string Webhook { get; set; } = string.Empty;

        // 可选签名密钥；不配置就不加签。
        public string Secret { get; set; } = string.Empty;

        // 逗号分隔的主机白名单；为空时拒绝任何地址。
        public string AllowedHosts { get; set; } = string.Empty;
    }
}
