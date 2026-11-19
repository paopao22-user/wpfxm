using System;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Inspections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IotGatewayLearningApi.Workers
{
    /// <summary>
    /// 巡检调度后台服务：不引入 Quartz，用 PeriodicTimer 每 30 秒扫一次。
    ///
    /// InspectionService 是 Scoped（依赖 DbContext），后台服务是 Singleton，
    /// 所以必须每轮自己 CreateScope，不能把 Scoped 服务注入构造函数。
    /// </summary>
    public sealed class InspectionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<InspectionWorker> _log;

        public InspectionWorker(IServiceScopeFactory scopes, ILogger<InspectionWorker> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    using var scope = _scopes.CreateScope();

                    try
                    {
                        await scope.ServiceProvider
                            .GetRequiredService<InspectionService>()
                            .TickAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 单轮失败不能让后台服务整体退出，否则后续巡检全部停摆。
                        _log.LogError(ex, "巡检调度本轮执行失败");
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
