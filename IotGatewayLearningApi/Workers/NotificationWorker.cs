using System;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Advisories;
using IotGatewayLearning.Domain.AI.Advisories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotGatewayLearningApi.Workers
{
    /// <summary>
    /// 通知发件箱后台服务：每 10 秒抢占一条待办投递。
    ///
    /// Notify:Enabled=false（默认）时直接跳过：
    /// 不抢占、不发请求、不增加 Attempts，待办保持 Pending，
    /// 这样本地演示不会误发给真实群，页面仍能查到待办。
    /// </summary>
    public sealed class NotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<NotificationWorker> _log;

        public NotificationWorker(IServiceScopeFactory scopes, ILogger<NotificationWorker> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    using var scope = _scopes.CreateScope();

                    var options = scope.ServiceProvider
                        .GetRequiredService<IOptions<NotificationOptions>>()
                        .Value;

                    if (!options.Enabled)
                    {
                        continue;
                    }

                    try
                    {
                        await scope.ServiceProvider
                            .GetRequiredService<AdvisoryService>()
                            .DeliverOnceAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "通知投递本轮执行失败");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止。
            }
        }
    }
}
