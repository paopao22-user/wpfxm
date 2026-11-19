using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;

namespace IotGatewayLearning.Application.AI
{
    public sealed partial class AiAnalysisService
    {
        // request 来自 HTTP；actor 来自 JWT；返回新分析或同一 UUID 已有结果。
        public async Task<AiAnalysisDetail> CreateAsync(
            CreateAiAnalysisRequest request,
            string actor,
            CancellationToken ct)
        {
            actor = RequireActor(actor);        // 校验已认证用户标识，防止匿名或超长用户名写入分析记录
            var device = Code(request.DeviceCode, 50);  // 清除首尾空格并校验长度；设备编码上限 50，点位编码上限 80（与数据库一致）
            var point = Code(request.PointCode, 80);

            if (!Guid.TryParse(request.RequestId, out var guid) || guid == Guid.Empty)  //`Guid.TryParse` 的作用是“尝试把字符串解析成 Guid”
                throw new AppException("requestId必须是非空UUID", 400);

            if (request.WindowMinutes is < 1 or > 10)
                throw new AppException("窗口必须为1至10分钟", 400);

            var requestId = guid.ToString("D");     // 将 Guid 转换为标准格式的字符串表示形式（32 个十六进制数字，分为五组，用连字符分隔）

            // 先检查重发，避免再次取样和重复调用模型。
            var existing = await _repository.FindRequestAsync(requestId, ct);   // 按 UUID 查重复请求；null 表示尚未创建记录。
            if (existing is not null)
                return Reuse(existing, actor, device, point, request.WindowMinutes);    // 判断“这次重发能不能复用旧记录”   

            var end = DateTime.UtcNow;      // 获取当前 UTC 时间作为分析窗口的结束时间
            var start = end.AddMinutes(-request.WindowMinutes);     // 计算分析窗口的开始时间，通过从结束时间减去请求的窗口分钟数来确定

            // 取真实采样后再做质量检查，客户端无法伪造这份模型输入。
            // null 表示设备不存在、已停用或点位未启用。
            var capture = await _repository.ReadWindowAsync(device, point, start, end, ct)  // 按设备和点位读取指定时间窗口内的采样数据，返回一个 AiCapture 对象，包含配置单位和样本列表
                ?? throw new AppException("设备或启用点位不存在", 404);

            // blockers 记录阻止模型调用的原因；数据不足也是可保存的业务结果。
            var input = BuildInput(device, point, start, end, capture, out var blockers); // 构建模型输入数据，并返回阻止模型调用的原因列表（blockers），如果有问题则会阻止模型调用

            var row = new AiAnalysisRecord
            {
                RequestId = requestId,
                CreatedBy = actor,
                DeviceCode = device,
                PointCode = point,
                WindowMinutes = request.WindowMinutes,
                WindowStart = start,
                WindowEnd = end,
                CreatedAt = end,
                InputJson = JsonSerializer.Serialize(input, Json),
                PromptVersion = PromptVersion,
                SystemPrompt = Instructions,
                ModelId = _settings.ModelId,
                Status = blockers.Count == 0 ? "Running" : "InsufficientData",
                ErrorCode = blockers.Count == 0 ? null : "InputQuality",
                ErrorMessage = blockers.Count == 0 ? null : string.Join("；", blockers),
                CompletedAt = blockers.Count == 0 ? null : end
            };

            // 数据库唯一索引负责并发裁决；只有创建成功的请求可进入模型阶段。
            if (!await _repository.TryAddAsync(row, ct))
            {
                existing = await _repository.FindRequestAsync(requestId, ct)        // 再查一次，避免并发重复请求的记录还没提交就抛 404
                    ?? throw new AppException("重复请求记录暂时不可见，请稍后查询", 409);

                return Reuse(existing, actor, device, point, request.WindowMinutes);    // 判断“这次重发能不能复用旧记录”
            }

            if (blockers.Count != 0)    // 数据不足或质量问题，直接返回已保存的 InsufficientData 记录。
                return Detail(row);     // 返回已保存的分析记录的详细信息，供客户端查看

            // 已先保存 Running 和输入快照；下面调用模型不占用数据库事务。
            var watch = Stopwatch.StartNew();           // 启动一个计时器，用于测量模型调用的耗时
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);   // 创建一个新的 CancellationTokenSource，并将传入的 ct 作为链接的取消令牌
            deadline.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));       // 设置一个超时时间，超过该时间后会触发取消操作

            try
            {
                ct.ThrowIfCancellationRequested();      // 在调用模型之前，检查传入的 CancellationToken 是否已被取消，如果已取消，则抛出 OperationCanceledException 异常

                // 先记录“尝试调用”；不据此保证模型服务已经接收或没有计费。
                row.ModelAttempted = true;
                await _repository.SaveAsync(ct);        // 保存模型调用尝试的状态到数据库

                var answer = await _model.CompleteAsync(row.SystemPrompt, row.InputJson, deadline.Token);   // 调用模型服务，传入系统提示和输入数据，并使用 deadline.Token 作为取消令牌，以便在超时或取消请求时中断模型调用

                // 模型返回时仍检查截止信号，不把已经取消的运行标记为成功。
                deadline.Token.ThrowIfCancellationRequested();

                row.Answer = answer.Text;
                row.InputTokens = answer.InputTokens;
                row.OutputTokens = answer.OutputTokens;
                row.Status = "Succeeded";
            }
            catch (OperationCanceledException)
            {
                row.Status = ct.IsCancellationRequested ? "Canceled" : "Failed";
                row.ErrorCode = ct.IsCancellationRequested ? "RequestCanceled" : "ModelTimeout";
                row.ErrorMessage = ct.IsCancellationRequested
                    ? "请求已取消；厂商处理和计费状态可能仍需核对"
                    : "模型分析超时";
            }
            catch (AiProviderException ex)
            {
                row.Status = "Failed";
                row.ErrorCode = ex.Code;
                row.ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                // 不把底层异常原文返回给用户；以记录 ID 关联服务端故障排查。
                row.Status = "Failed";
                row.ErrorCode = "AnalysisFailed";
                row.ErrorMessage = "分析执行失败，请联系管理员核查";
            }

            row.ElapsedMs = watch.ElapsedMilliseconds;
            row.CompletedAt = DateTime.UtcNow;

            // 浏览器或 WPF 取消后，仍给最终状态落库一个独立的有限时间。
            using var persist = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _repository.SaveAsync(persist.Token);     // 保存最终分析结果到数据库，即使请求已被取消，也会尝试在有限时间内完成保存操作

            return Detail(row);
        }

        // 重发只允许相同创建者和相同业务参数，不能借 UUID 切换分析对象。
        private static AiAnalysisDetail Reuse(
            AiAnalysisRecord row,
            string actor,
            string device,
            string point,
            int minutes)
        {
            if (row.CreatedBy != actor || row.DeviceCode != device ||
                row.PointCode != point || row.WindowMinutes != minutes)
                throw new AppException("requestId已用于其他分析请求", 409);

            // 包括 Running 和失败记录；重发不再次调用模型。
            return Detail(row);
        }

        // out blockers 同时返回质量问题；返回值则是可追溯的输入快照。
        private static AiAnalysisInput BuildInput(
            string device,
            string point,
            DateTime start,
            DateTime end,
            AiCapture capture,
            out List<string> blockers)
        {
            blockers = new List<string>();

            var samples = capture.Samples
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.Id)
                .Select(x => x with { OccurredAt = Utc(x.OccurredAt) })
                .ToList();

            // 只让 Good 且有值的样本参与统计；Bad 样本仍保留用于核查。
            var good = samples
                .Where(x => x.Quality == "Good" && x.Value.HasValue)
                .ToList();

            var unit = capture.ConfiguredUnit?.Trim();

            // 使用区分大小写的单位比较，避免把不同工程单位混合求平均。
            var unitsValid = !string.IsNullOrEmpty(unit) && good.Count > 0 &&
                good.All(x => string.Equals(x.Unit?.Trim(), unit, StringComparison.Ordinal));

            if (samples.Count > 120)
                blockers.Add("窗口超过120条，请缩小窗口或先完善聚合方案");

            if (good.Count < 2)
                blockers.Add("至少需要2条有效样本");

            if (!unitsValid)
                blockers.Add("单位缺失或与点位配置不一致");

            if (samples.LastOrDefault() is not { Quality: "Good", Value: not null } latest)
                blockers.Add("最新样本缺失或质量不可用");
            else if (end - latest.OccurredAt > TimeSpan.FromSeconds(60))
                blockers.Add("最新样本超过本章60秒新鲜度要求");

            // 输入超过上限或单位混杂时，不输出误导性的窗口统计。
            var aggregate = unitsValid && samples.Count <= 120;

            return new AiAnalysisInput(
                device,
                point,
                start,
                end,
                unit,
                samples.Count,
                good.Count,
                aggregate ? good.Min(x => x.Value) : null,
                aggregate ? good.Max(x => x.Value) : null,
                aggregate ? good.Average(x => x.Value) : null,
                samples,
                new[]
                {
                    "本章未提供设备阈值、工况和手册，不得判定超限或安全运行",
                    "样本数不代表采样完整率；质量不为Good的记录仅供核查",
                    "超过120条时只保留最新121条用于说明超限，不作为完整窗口分析"
                });
        }
    }
}
