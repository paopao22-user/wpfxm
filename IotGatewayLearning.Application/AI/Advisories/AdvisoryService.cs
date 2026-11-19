using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.AI.Inspections;
using IotGatewayLearning.Application.AI.Mcp;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI;
using IotGatewayLearning.Domain.AI.Advisories;
using IotGatewayLearning.Domain.AI.Mcp;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IotGatewayLearning.Application.AI.Advisories
{
    /// <summary>
    /// AI 告警研判与通知分发。
    ///
    /// 与 Day31 的关系：Day31 阈值规则负责确定性告警和联锁，
    /// 本章只是补充研判。模型说"正常"不能自动恢复规则告警，
    /// 也不能给 PLC 下发任何指令。
    /// </summary>
    public sealed class AdvisoryService
    {
        private const string SystemPrompt = """
            你是设备告警研判助手。只输出一个 JSON 对象，不要输出 Markdown 代码块、解释或多余文字。
            格式：{"needsAttention":true,"summary":"不超过1000字","advice":"不超过2000字"}
            要求：只根据给定的真实证据和资料回答；资料缺失时在 summary 中说明；
            不得编造数据；不得给出设备控制指令。
            """;

        private readonly IAdvisoryRepository _repo;
        private readonly InspectionService _inspections;
        private readonly McpAccessService _mcp;
        private readonly INotificationSender _sender;
        private readonly IAiTextClient _ai;
        private readonly ILogger<AdvisoryService> _log;

        public AdvisoryService(
            IAdvisoryRepository repo,
            InspectionService inspections,
            McpAccessService mcp,
            INotificationSender sender,
            IAiTextClient ai,
            ILogger<AdvisoryService> log)
        {
            _repo = repo;
            _inspections = inspections;
            _mcp = mcp;
            _sender = sender;
            _ai = ai;
            _log = log;
        }

        public async Task<AiAdvisory> CreateAsync(
            CreateAdvisory request,
            string actor,
            CancellationToken ct)
        {
            // ① 巡检记录必须属于当前用户，且已完成并留有证据。
            var run = await _inspections.RunAsync(request.RunId, actor, ct)
                ?? throw new AppException("巡检记录不存在", 404);

            if (run.State != "Succeeded" || string.IsNullOrEmpty(run.EvidenceJson))
            {
                throw new AppException("需要已完成且有证据的巡检", 400);
            }

            // ② 旧证据 ID 不能成为越权凭证：创建前重新走一次 Day47 权限检查。
            await _mcp.InvokeAsync(
                actor,
                new McpInvokeRequest(run.DeviceCode, "get_device"),
                ct);

            // ③ 同一 RunId 只研判一次；重复提交返回旧记录，包括 Failed。
            var existing = await _repo.ByRunAsync(request.RunId, ct);
            if (existing is not null)
            {
                return existing;
            }

            // ④ 汇总证据。知识库引用校验失败在这里直接 400，不会留下 Running 垃圾记录。
            var evidence = await _repo.EvidenceAsync(run, request.KnowledgeAnswerId, ct);
            var evidenceJson = JsonSerializer.Serialize(evidence);

            var row = new AiAdvisory
            {
                RunId = run.Id,
                Owner = actor,
                DeviceCode = run.DeviceCode,
                State = "Running",
                EvidenceJson = evidenceJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ⑤ 先落 Running 占住 RunId 唯一键，再由模型研判。
            if (!await _repo.StartAsync(row, ct))
            {
                return await _repo.ByRunAsync(request.RunId, ct)
                    ?? throw new AppException("记录不存在或状态已变化，请刷新", 409);
            }

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

                var result = await _ai.CompleteAsync(
                    SystemPrompt,
                    BuildInput(run, evidenceJson),
                    linked.Token);

                var verdict = Parse(result.Text);

                row.State = verdict.NeedsAttention ? "Active" : "NoAttention";
                row.Summary = verdict.Summary;
                row.Advice = verdict.Advice;

                // 只有"需要关注"才建通知待办。
                await CompleteAsync(row, verdict.NeedsAttention);
            }
            catch (Exception ex)
            {
                // 失败绝不等于"无需关注"：不建通知待办，状态记 Failed。
                row.State = "Failed";
                row.Summary = "研判未完成，请核对证据及模型配置";
                row.Advice = string.Empty;

                await CompleteAsync(row, notify: false);

                _log.LogWarning(ex, "告警研判 {AdvisoryId} 失败", row.Id);
            }

            return row;
        }

        public Task<List<AiAdvisory>> ListAsync(string actor, CancellationToken ct) =>
            _repo.ListAsync(actor, ct);

        public async Task<bool> HandleAsync(
            string id,
            string actor,
            bool close,
            string remark,
            CancellationToken ct)
        {
            var text = (remark ?? string.Empty).Trim();

            if (text.Length == 0 || text.Length > 500)
            {
                throw new AppException("需要处理备注，最多500字", 400);
            }

            var ok = await _repo.HandleAsync(id, actor, close, text, ct);

            if (!ok)
            {
                // Active 直接关闭、重复确认、别人的记录，都归到这里。
                throw new AppException("记录不存在或状态已变化，请刷新", 409);
            }

            return true;
        }

        public Task<List<AiNotification>> NotificationsAsync(string actor, CancellationToken ct) =>
            _repo.NotificationsAsync(actor, ct);

        /// <summary>
        /// 通知 Worker 每轮调用一次：抢占一条待办并投递。
        /// 投递失败只重试通知，不重新调用模型。
        /// </summary>
        public async Task DeliverOnceAsync(CancellationToken ct)
        {
            var notification = await _repo.ClaimAsync(ct);
            if (notification is null)
            {
                return;
            }

            var advisory = await _repo.GetAsync(notification.AdvisoryId, ct);

            // 记录被删或已关闭，就没有必要再打扰别人。
            if (advisory is null || (advisory.State != "Active" && advisory.State != "Acknowledged"))
            {
                await _repo.DeliveredAsync(notification, "Failed", "研判记录不存在或已关闭", ct);
                return;
            }

            try
            {
                await _sender.SendAsync(advisory, ct);

                // Accepted 只代表第三方接收，不代表人员已读。
                await _repo.DeliveredAsync(notification, "Accepted", string.Empty, ct);
            }
            catch (Exception ex)
            {
                var state = notification.Attempts < 3 ? "Retry" : "Failed";

                await _repo.DeliveredAsync(notification, state, "通知投递失败", ct);

                _log.LogWarning(ex, "通知待办 {NotificationId} 投递失败", notification.Id);
            }
        }

        private async Task CompleteAsync(AiAdvisory row, bool notify)
        {
            // 落库用独立 5 秒 CTS：主调用被取消时也要尽量写回终态。
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _repo.CompleteAsync(row, notify, cts.Token);
        }

        private static string BuildInput(InspectionRun run, string evidenceJson) => $"""
            设备：{run.DeviceCode}
            点位：{run.PointCode}
            巡检问题：{run.Question}
            巡检回答：{run.Answer}
            证据与资料（JSON）：
            {evidenceJson}
            """;

        private static (bool NeedsAttention, string Summary, string Advice) Parse(string text)
        {
            var json = (text ?? string.Empty).Trim();

            // 模型偶尔会套一层 ```json 代码块，剥掉再解析；不猜字段、不补默认值。
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                var start = json.IndexOf('\n');
                var end = json.LastIndexOf("```", StringComparison.Ordinal);

                if (start >= 0 && end > start)
                {
                    json = json[(start + 1)..end].Trim();
                }
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!TryBool(root, "needsAttention", out var needsAttention)
                || !TryString(root, "summary", out var summary)
                || !TryString(root, "advice", out var advice))
            {
                throw new InvalidOperationException("模型输出缺少必需字段");
            }

            if (summary.Length < 1 || summary.Length > 1000
                || advice.Length < 1 || advice.Length > 2000)
            {
                throw new InvalidOperationException("模型输出长度不合规");
            }

            return (needsAttention, summary, advice);
        }

        // 同时接受 camelCase 与 PascalCase，其余类型一律视为缺失。
        private static bool TryBool(JsonElement root, string name, out bool value)
        {
            value = false;

            if (!root.TryGetProperty(name, out var element)
                && !root.TryGetProperty(Pascal(name), out element))
            {
                return false;
            }

            if (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
            {
                return false;
            }

            value = element.GetBoolean();
            return true;
        }

        private static bool TryString(JsonElement root, string name, out string value)
        {
            value = string.Empty;

            if (!root.TryGetProperty(name, out var element)
                && !root.TryGetProperty(Pascal(name), out element))
            {
                return false;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = element.GetString() ?? string.Empty;
            return true;
        }

        private static string Pascal(string name) =>
            char.ToUpperInvariant(name[0]) + name[1..];
    }
}
