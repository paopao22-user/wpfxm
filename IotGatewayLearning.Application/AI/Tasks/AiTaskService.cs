using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;

namespace IotGatewayLearning.Application.AI.Tasks
{
    public sealed class AiTaskService : IAiTaskService
    {
        private readonly IAiTaskRepository _repo;
        private readonly IAiDeviceQuery _queries;
        private readonly IAiTaskRunner _runner;

        public AiTaskService(
            IAiTaskRepository repo,
            IAiDeviceQuery queries,
            IAiTaskRunner runner)
        {
            _repo = repo;
            _queries = queries;
            _runner = runner;
        }

        // 同一请求键只允许同一身份、同一设备、同一点位和同一问题。
        private static AiToolTask Reuse(AiToolTask row, StartTask request, string actor)
        {
            if (row.Actor != actor ||
                row.DeviceCode != request.DeviceCode ||
                row.PointCode != request.PointCode ||
                row.Question != request.Question)
                throw new AppException("请求编号已用于其他任务", 409);

            return row;
        }

        public async Task<AiToolTask> CreateAsync(
            StartTask request,
            string actor,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100)
                throw new AppException("身份无效", 401);

            if (string.IsNullOrWhiteSpace(request.RequestKey) ||            // ① 参数校验（长度、非空）
                request.RequestKey.Length > 100 ||
                request.RequestKey.Any(c => c > 127) ||
                string.IsNullOrWhiteSpace(request.DeviceCode) ||
                request.DeviceCode.Length > 50 ||
                string.IsNullOrWhiteSpace(request.PointCode) ||
                request.PointCode.Length > 80 ||
                string.IsNullOrWhiteSpace(request.Question) ||
                request.Question.Length > 1000)
                throw new AppException("任务参数无效", 400);

            // 查重先于模型调用：刷新或网络重发不会重复收费。
            var old = await _repo.ByKeyAsync(request.RequestKey, ct);       // ② ByKeyAsync 查重 → 命中就 Reuse / 冲突 409 
            if (old is not null)
                return Reuse(old, request, actor);

            // 创建前验证设备存在且启用，避免留下不存在设备的调查记录。
            await _queries.DeviceAsync(request.DeviceCode, ct);             // ③ DeviceAsync 验设备存在且启用

            var task = new AiToolTask
            {
                RequestKey = request.RequestKey,
                Actor = actor,
                DeviceCode = request.DeviceCode,
                PointCode = request.PointCode,
                Question = request.Question,
                CreatedAt = DateTime.UtcNow
            };

            if (!await _repo.AddAsync(task, ct))                            // ④ INSERT 一行 Running（唯一索引兜并发）
            {
                var concurrent = await _repo.ByKeyAsync(request.RequestKey, ct);
                return Reuse(
                    concurrent ?? throw new AppException("并发任务暂不可见", 409),
                    request,
                    actor);
            }

            // 同时响应客户端取消和服务端截止时间，限制单次任务资源占用。
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);    // ⑤ 建 Linked CTS，120 秒
            timeout.CancelAfter(TimeSpan.FromSeconds(120));                             

            using var tools = new DeviceQueryTools(                         // ⑥ new DeviceQueryTools(设备/点位/窗口/落库回调)  创建工具
                _queries,
                task.DeviceCode,        // 设备锁死
                task.PointCode,         // 点位锁死
                task.CreatedAt,         // 时间窗锁死
                async calls =>
                {
                    // 每执行一步就固化真实工具结果，页面回读与模型看到的数据一致。
                    task.CallsJson = JsonSerializer.Serialize(calls);

                    // 请求取消后也给日志一个有限的保存期限。
                    using var persist = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _repo.SaveAsync(persist.Token);
                },
                timeout.Token);

            try
            {
                task.Answer = await _runner.RunAsync(task.Question, tools, timeout.Token);      // ⑦ await _runner.RunAsync(question, tools, token)
                timeout.Token.ThrowIfCancellationRequested();
                task.Status = "Succeeded";                                                      // ⑧ 写终态 Succeeded / Canceled / Failed
            }
            catch (OperationCanceledException)
            {
                task.Status = "Canceled";
                task.ErrorMessage = "任务取消或超时";
            }
            catch (Exception)
            {
                // 详细原因已写入 calls_json；对外只给可公开的提示。
                task.Status = "Failed";
                task.ErrorMessage = "调查未完成，请核对工具记录与服务配置";
            }

            task.CompletedAt = DateTime.UtcNow;

            using var finalSave = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _repo.SaveAsync(finalSave.Token);                         // ⑨ SaveAsync 最终落库

            return task;
        }

        public Task<List<AiToolTask>> ListAsync(string actor, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100)
                throw new AppException("身份无效", 401);

            return _repo.ListAsync(actor, ct);
        }

        public async Task<AiToolTask> GetAsync(long id, string actor, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100)
                throw new AppException("身份无效", 401);

            if (id <= 0)
                throw new AppException("任务编号无效", 400);

            return await _repo.GetAsync(id, actor, ct)
                ?? throw new AppException("任务不存在或无权访问", 404);
        }
    }
}
