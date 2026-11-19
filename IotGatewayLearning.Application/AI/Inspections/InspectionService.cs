using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Mcp;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI;
using IotGatewayLearning.Domain.AI.Mcp;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IotGatewayLearning.Application.AI.Inspections
{
    /// <summary>
    /// 巡检调度与执行。
    ///
    /// 三个入口共用同一套队列：
    ///   手动 = 当前 JWT 用户，且必须是计划所有者；
    ///   定时 = 计划保存的 Owner；
    ///   事件 = 受认证的设备桥接，执行身份仍取计划 Owner。
    ///
    /// 并发控制全部落在数据库：merge_key 唯一索引做窗口合并，
    /// 条件 UPDATE 做抢占，不依赖进程内锁。
    /// </summary>
    public sealed class InspectionService
    {
        private static readonly string[] RequiredTools = { "get_device", "get_history", "get_alarms" };

        private const string SystemPrompt =
            "你是设备巡检助手。只根据给定的真实证据回答，不得编造数据，不得给出设备控制指令；" +
            "证据不足时要明确说明缺什么，并提示人工核查。回答控制在 300 字以内。";

        private readonly IInspectionRepository _repo;
        private readonly IMcpRepository _mcpRepo;
        private readonly McpAccessService _mcp;
        private readonly IAiTextClient _ai;
        private readonly ILogger<InspectionService> _log;

        public InspectionService(
            IInspectionRepository repo,
            IMcpRepository mcpRepo,
            McpAccessService mcp,
            IAiTextClient ai,
            ILogger<InspectionService> log)
        {
            _repo = repo;
            _mcpRepo = mcpRepo;
            _mcp = mcp;
            _ai = ai;
            _log = log;
        }

        /// <summary>
        /// Worker 每 30 秒调用一次：先把到期计划入队，再抢占并执行一个任务。
        /// 每轮最多开始一项，单实例下天然限流。
        /// </summary>
        public async Task TickAsync(CancellationToken ct)
        {
            foreach (var plan in await _repo.DueAsync(ct))
            {
                // 先推进计划时间，避免本轮异常导致同一计划被反复入队。
                await _repo.AdvanceAsync(plan, ct);
                await EnqueueAsync(plan, "Scheduled", DateTimeOffset.UtcNow, ct);
            }

            var run = await _repo.ClaimAsync(ct);
            if (run is null)
            {
                return;
            }

            await ExecuteAsync(run, ct);
        }

        public async Task<InspectionPlan> CreateAsync(
            SaveInspection request,
            string actor,
            CancellationToken ct)
        {
            var device = request.DeviceCode.Trim();
            var point = request.PointCode.Trim();
            var question = request.Question.Trim();

            // 先确认设备真实存在，否则"设备不可用"会被后面的授权检查掩盖成 403。
            if (!await _mcpRepo.DeviceExistsAsync(device, ct))
            {
                throw new AppException("设备不可用", 400);
            }

            // 创建时检查一次授权；执行时还会再检查，防止撤权后旧计划继续读取。
            await RequireToolsAsync(actor, device, ct);

            var plan = new InspectionPlan
            {
                Owner = actor,
                DeviceCode = device,
                PointCode = point,
                Question = question,
                IntervalSeconds = request.IntervalSeconds,
                Enabled = true,
                NextAt = DateTime.UtcNow
            };

            return await _repo.AddPlanAsync(plan, ct);
        }

        public Task<List<InspectionPlan>> PlansAsync(string actor, CancellationToken ct) =>
            _repo.PlansAsync(actor, ct);

        /// <summary>
        /// 立即执行一次。source 由入口决定（Manual），窗口桶仍按计划周期计算。
        /// </summary>
        public async Task<InspectionRun> SubmitAsync(
            long id,
            string actor,
            string source,
            DateTimeOffset at,
            CancellationToken ct)
        {
            var plan = await _repo.PlanAsync(id, ct)
                ?? throw new AppException("计划不存在", 404);

            if (plan.Owner != actor)
            {
                throw new AppException("计划不属于当前用户", 403);
            }

            if (!plan.Enabled)
            {
                throw new AppException("计划未启用", 403);
            }

            return await EnqueueAsync(plan, source, at, ct);
        }

        public async Task<bool> EnableAsync(
            long id,
            string actor,
            bool value,
            CancellationToken ct)
        {
            var ok = await _repo.EnableAsync(id, actor, value, ct);

            if (!ok)
            {
                throw new AppException("计划不存在或不属于当前用户", 404);
            }

            return true;
        }

        /// <summary>
        /// 设备事件入口：只接受实时窗口内的事件。
        /// 设备不能指定 Owner，执行身份永远取计划保存的所有者。
        /// </summary>
        public async Task<InspectionRun> EventAsync(InspectionEvent request, CancellationToken ct)
        {
            var plan = await _repo.PlanAsync(request.PlanId, ct)
                ?? throw new AppException("计划不存在", 404);

            if (!plan.Enabled)
            {
                throw new AppException("计划未启用", 403);
            }

            var at = request.OccurredAt.ToUniversalTime();
            var now = DateTime.UtcNow;

            // 旧消息超过十分钟拒绝实时分析，避免把历史数据伪装成当前异常。
            if (at > now.AddSeconds(30) || at < now.AddMinutes(-10))
            {
                throw new AppException("事件不在实时分析接收窗口", 400);
            }

            return await EnqueueAsync(plan, "Event", at, ct);
        }

        public Task<List<InspectionRun>> RunsAsync(string actor, CancellationToken ct) =>
            _repo.RunsAsync(actor, ct);

        public Task<InspectionRun?> RunAsync(string id, string actor, CancellationToken ct) =>
            _repo.RunAsync(id, actor, ct);

        // ==============================
        // 内部实现
        // ==============================

        private async Task<InspectionRun> EnqueueAsync(
            InspectionPlan plan,
            string source,
            DateTimeOffset at,
            CancellationToken ct)
        {
            // 合并窗口最小 60 秒；手动、定时、事件共用同一个桶算法。
            var window = Math.Max(60, plan.IntervalSeconds);
            var bucket = at.ToUnixTimeSeconds() / window;

            var run = new InspectionRun
            {
                PlanId = plan.Id,
                Owner = plan.Owner,
                DeviceCode = plan.DeviceCode,
                PointCode = plan.PointCode,
                Question = plan.Question,
                MergeKey = $"plan/{plan.Id}/window/{bucket}",
                Source = source,
                State = "Queued",
                CreatedAt = DateTime.UtcNow,
                NextAt = DateTime.UtcNow
            };

            return await _repo.EnqueueAsync(run, ct);
        }

        private async Task ExecuteAsync(InspectionRun run, CancellationToken ct)
        {
            var plan = await _repo.PlanAsync(run.PlanId, ct);

            // 停用只阻止新调度；已排队的任务在执行前再检查一次。
            if (plan is null || !plan.Enabled)
            {
                await CancelAsync(run, "计划停用");
                return;
            }

            try
            {
                await RequireToolsAsync(run.Owner, run.DeviceCode, ct);
            }
            catch (AppException)
            {
                await CancelAsync(run, "设备或授权不可用，请核对");
                return;
            }

            try
            {
                // 首次执行冻结证据；重试复用同一份，避免"证据换了但问题没换"。
                if (string.IsNullOrEmpty(run.EvidenceJson))
                {
                    var evidence = await BuildEvidenceAsync(run, ct);
                    await _repo.EvidenceAsync(run.Id, evidence, ct);
                    run.EvidenceJson = evidence;
                }

                // 主执行 150 秒；落库另用独立 5 秒 CTS，超时后仍能写回终态。
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

                var answer = await _ai.CompleteAsync(SystemPrompt, BuildInput(run), linked.Token);

                run.State = "Succeeded";
                run.Answer = answer.Text;
                run.Error = string.Empty;
                run.CompletedAt = DateTime.UtcNow;

                await FinishAsync(run);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 服务正在停止：不写终态，等租约过期由 ClaimAsync 置 Unknown。
                throw;
            }
            catch (Exception ex)
            {
                // 模型超时、服务不可用、证据读取失败都走同一条有限重试策略。
                run.State = run.Attempts < 3 ? "Retry" : "Failed";
                run.NextAt = DateTime.UtcNow.AddSeconds(30 * run.Attempts);
                run.CompletedAt = run.State == "Failed" ? DateTime.UtcNow : null;
                run.Error = "分析未完成；已保留证据，按策略有限重试";

                await FinishAsync(run);

                _log.LogWarning(ex, "巡检任务 {RunId} 第 {Attempts} 次执行失败", run.Id, run.Attempts);
            }
        }

        private async Task<string> BuildEvidenceAsync(InspectionRun run, CancellationToken ct)
        {
            // 三次调用都走 Day47 的同一入口：权限、审计、状态一处不少。
            var device = await _mcp.InvokeAsync(
                run.Owner,
                new McpInvokeRequest(run.DeviceCode, "get_device"),
                ct);

            var history = await _mcp.InvokeAsync(
                run.Owner,
                new McpInvokeRequest(run.DeviceCode, "get_history", run.PointCode),
                ct);

            var alarms = await _mcp.InvokeAsync(
                run.Owner,
                new McpInvokeRequest(run.DeviceCode, "get_alarms"),
                ct);

            // 证据里保留 get_history 返回的真实 windowEnd，
            // 它和执行时刻不是一回事：排队延迟必须能从证据里看出来。
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["device"] = device.Data,
                ["history"] = history.Data,
                ["alarms"] = alarms.Data
            });
        }

        private static string BuildInput(InspectionRun run) => $"""
            设备：{run.DeviceCode}
            点位：{run.PointCode}
            问题：{run.Question}
            真实证据（JSON）：
            {run.EvidenceJson}
            """;

        private async Task RequireToolsAsync(string owner, string device, CancellationToken ct)
        {
            if (!await _mcpRepo.ActorExistsAsync(owner, ct))
            {
                throw new AppException("计划所有者已停用", 403);
            }

            foreach (var tool in RequiredTools)
            {
                if (!await _mcpRepo.AllowedAsync(owner, device, tool, ct))
                {
                    throw new AppException("巡检需要设备资料、历史、告警三项授权", 403);
                }
            }
        }

        private async Task CancelAsync(InspectionRun run, string reason)
        {
            run.State = "Canceled";
            run.Error = reason;
            run.CompletedAt = DateTime.UtcNow;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _repo.FinishAsync(run, cts.Token);
        }

        private async Task FinishAsync(InspectionRun run)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _repo.FinishAsync(run, cts.Token);
        }
    }
}
