using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Application.DTOs.Control;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;

namespace IotGatewayLearning.Application.Services.Implementations
{
    /// <summary>
    /// 控制申请审批。
    ///
    /// 本章只做申请、审批与审计：
    /// 提交成功不发命令，审批通过也不发命令，真正执行属于 Day51。
    /// 关机申请也只是"控制请求"，不替代 PLC 联锁和现场急停。
    /// </summary>
    public sealed class ControlRequestService : IControlRequestService
    {
        // 控制白名单写死在代码里，不接受任意设备、任意动作。
        private const string WhitelistedDevice = "AC-001";

        private static readonly string[] AllowedActions = { "setTemperature", "powerOff" };

        private readonly IControlRepository _repo;

        public ControlRequestService(IControlRepository repo) => _repo = repo;

        public async Task<ControlRequest> SubmitAsync(
            SubmitControl request,
            string actor,
            CancellationToken ct)
        {
            var key = (request.RequestKey ?? string.Empty).Trim();

            if (!Guid.TryParseExact(key, "N", out _))
            {
                throw new AppException("请求键必须为32位UUID", 400);
            }

            var device = (request.DeviceCode ?? string.Empty).Trim();
            var action = (request.Action ?? string.Empty).Trim();
            var reason = RequireText(request.Reason);

            if (!string.Equals(device, WhitelistedDevice, StringComparison.Ordinal))
            {
                throw new AppException("设备不在控制白名单", 400);
            }

            if (Array.IndexOf(AllowedActions, action) < 0)
            {
                throw new AppException("动作不在控制白名单", 400);
            }

            CheckTarget(action, request.Target);

            if (!await _repo.DeviceExistsAsync(device, ct))
            {
                throw new AppException("设备不可用", 400);
            }

            // 幂等：同 Key 同参数返回原申请；同 Key 不同参数是客户端错误。
            var existing = await _repo.ByKeyAsync(key, ct);
            if (existing is not null)
            {
                EnsureSame(existing, actor, device, action, request.Target, reason);
                return existing;
            }

            var row = new ControlRequest
            {
                RequestKey = key,
                DeviceCode = device,
                Action = action,
                Target = action == "setTemperature" ? request.Target : null,
                Reason = reason,
                Requester = actor,
                Reviewer = string.Empty,
                State = "Pending",
                CreatedAt = DateTime.UtcNow,
                ReviewBefore = DateTime.UtcNow.AddMinutes(30),
                ExecuteBefore = null,
                UpdatedAt = DateTime.UtcNow
            };

            var saved = await _repo.AddAsync(row, ct);

            // 并发情况下别的实例可能先用掉这个 Key，且参数不同。
            EnsureSame(saved, actor, device, action, request.Target, reason);

            return saved;
        }

        public async Task<List<ControlRequest>> ListAsync(
            string actor,
            bool review,
            bool canApprove,
            CancellationToken ct)
        {
            if (review && !canApprove)
            {
                throw new AppException("没有审批队列权限", 403);
            }

            return await _repo.ListAsync(actor, review, ct);
        }

        public async Task<object> DetailAsync(
            string id,
            string actor,
            bool canApprove,
            CancellationToken ct)
        {
            var row = await _repo.FindAsync(id, ct)
                ?? throw new AppException("申请不存在", 404);

            // 非本人且非审批人，统一 404，不泄露别人的申请是否存在。
            if (row.Requester != actor && !canApprove)
            {
                throw new AppException("申请不存在", 404);
            }

            return new
            {
                request = row,
                audits = await _repo.AuditsAsync(id, ct)
            };
        }

        public async Task<ControlRequest> DecideAsync(
            string id,
            string actor,
            bool approve,
            string remark,
            CancellationToken ct)
        {
            var row = await _repo.FindAsync(id, ct)
                ?? throw new AppException("申请不存在", 404);

            // Service 先挡一次自审，数据库条件更新里还有 Requester != actor 兜底。
            if (row.Requester == actor)
            {
                throw new AppException("不能审批自己的申请", 403);
            }

            var text = RequireText(remark);

            // 批准前重新核对设备与目标：申请提交后设备可能已停用或被移出白名单。
            if (approve)
            {
                if (!string.Equals(row.DeviceCode, WhitelistedDevice, StringComparison.Ordinal))
                {
                    throw new AppException("设备不在控制白名单", 400);
                }

                CheckTarget(row.Action, row.Target);

                if (!await _repo.DeviceExistsAsync(row.DeviceCode, ct))
                {
                    throw new AppException("设备不可用", 400);
                }
            }

            var ok = await _repo.DecideAsync(id, actor, approve, text, ct);

            if (!ok)
            {
                throw new AppException("申请已处理或超过审批有效期，请刷新", 409);
            }

            return await _repo.FindAsync(id, ct) ?? row;
        }

        public async Task<ControlRequest> CancelAsync(
            string id,
            string actor,
            string remark,
            CancellationToken ct)
        {
            var text = RequireText(remark);

            var ok = await _repo.CancelAsync(id, actor, text, ct);

            if (!ok)
            {
                throw new AppException("申请不存在、非本人或已经开始执行", 409);
            }

            return await _repo.FindAsync(id, ct)
                ?? throw new AppException("申请不存在", 404);
        }

        private static void CheckTarget(string action, decimal? target)
        {
            if (action == "setTemperature")
            {
                if (target is null
                    || target.Value < 16m
                    || target.Value > 30m
                    || decimal.Round(target.Value, 1) != target.Value)
                {
                    throw new AppException("设定值必须为16至30℃，最多一位小数", 400);
                }

                return;
            }

            // powerOff 不允许带目标值。
            if (target is not null)
            {
                throw new AppException("仅允许设温或不带目标值的关机申请", 400);
            }
        }

        private static string RequireText(string? value)
        {
            var text = (value ?? string.Empty).Trim();

            if (text.Length == 0 || text.Length > 500)
            {
                throw new AppException("说明必填，最多500字", 400);
            }

            return text;
        }

        private static void EnsureSame(
            ControlRequest row,
            string actor,
            string device,
            string action,
            decimal? target,
            string reason)
        {
            if (row.Requester != actor
                || row.DeviceCode != device
                || row.Action != action
                || row.Target != (action == "setTemperature" ? target : null)
                || row.Reason != reason)
            {
                throw new AppException("请求键已用于不同申请", 409);
            }
        }
    }
}
