using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.AI.Advisories;
using IotGatewayLearning.Domain.Entities;
using Microsoft.Extensions.Options;

namespace IotGatewayLearningApi.AI.Advisories
{
    /// <summary>
    /// 通用 Webhook 通知实现（钉钉方案的最小替代）。
    ///
    /// 放在 WebAPI 而不是 Infrastructure：它需要 IHttpClientFactory，
    /// 而 Application/Infrastructure 没有引入 Microsoft.Extensions.Http。
    /// Webhook 与密钥只从服务端配置读取，不进 WPF、不进日志、不进模型 Prompt。
    /// </summary>
    public sealed class WebhookNotificationSender : INotificationSender
    {
        private readonly IHttpClientFactory _clients;
        private readonly NotificationOptions _options;

        public WebhookNotificationSender(
            IHttpClientFactory clients,
            IOptions<NotificationOptions> options)
        {
            _clients = clients;
            _options = options.Value;
        }

        public async Task SendAsync(AiAdvisory advisory, CancellationToken ct)
        {
            var uri = BuildUri(_options);

            // 只发编号和摘要：不外发手册全文、采样全集和任何凭据。
            var text = $"AI提示 {advisory.Id}\n设备：{advisory.DeviceCode}\n{advisory.Summary}\n请进入平台查看证据。";

            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(new { text })
            };

            if (!string.IsNullOrWhiteSpace(_options.Secret))
            {
                request.Headers.Add("X-Notify-Sign", Sign(_options.Secret, text));
            }

            var client = _clients.CreateClient("Notify");

            using var response = await client.SendAsync(request, ct);

            // HTTP 200 不等于业务成功；EnsureSuccessStatusCode 只覆盖传输层。
            response.EnsureSuccessStatusCode();
        }

        private static Uri BuildUri(NotificationOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Webhook))
            {
                throw new InvalidOperationException("未配置通知地址");
            }

            if (!Uri.TryCreate(options.Webhook, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new InvalidOperationException("通知地址必须是 HTTPS");
            }

            // 主机白名单：白名单为空时拒绝任何地址，而不是默认放行。
            var allowed = options.AllowedHosts
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!allowed.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("通知主机不在白名单");
            }

            return uri;
        }

        private static string Sign(string secret, string text)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
