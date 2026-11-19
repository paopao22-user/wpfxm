using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.AI.Mcp;
using IotGatewayLearning.Domain.AI.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;

namespace IotGatewayLearning.Application.AI.Mcp
{
    /// <summary>
    /// 设备工具的唯一执行入口。
    ///
    /// 本章没有引入 ModelContextProtocol 包，也没有暴露 /mcp 协议端点：
    /// 工具目录 Catalog 是进程内静态白名单，HTTP 管理接口和进程内 AI Host
    /// 都必须经过 InvokeAsync，权限、审计、状态检查一处不少。
    /// 将来接入真正的 MCP 服务端时，工具方法只需转调本类，不复制业务逻辑。
    /// </summary>
    public sealed class McpAccessService
    {
        /// <summary>
        /// 只读工具白名单。写死在代码里，不接受数据库配置、用户上传脚本或任意工具名。
        /// </summary>
        public static readonly ToolInfo[] Catalog =
        {
            new("get_device", "查询设备台账资料，不执行任何控制", "deviceCode:string"),
            new("get_history", "查询某点位最近 1 分钟采样，最多 60 条", "deviceCode:string,pointCode:string"),
            new("get_alarms", "查询设备当前活动与已确认告警，不自动确认", "deviceCode:string")
        };

        private readonly IMcpRepository _repo;
        private readonly IAiDeviceQuery _query;

        public McpAccessService(IMcpRepository repo, IAiDeviceQuery query)
        {
            _repo = repo;
            _query = query;
        }

        public IReadOnlyList<ToolInfo> Tools => Catalog;

        public Task<List<McpGrant>> GrantsAsync(CancellationToken ct) => _repo.GrantsAsync(ct);

        public async Task ChangeAsync(
            McpGrantRequest request,
            string operatorName,
            bool grant,
            CancellationToken ct)
        {
            // 白名单之外的授权一律拒绝，避免把任意方法名塞进权限表。
            if (!Catalog.Any(x => x.Name == request.Tool))
            {
                throw new AppException("工具不在白名单内", 400);
            }

            // 目录可见不等于任何设备可读：授权前先确认账号和设备都还在有效状态。
            if (!await _repo.ActorExistsAsync(request.Actor, ct))
            {
                throw new AppException("被授权账号不存在或已停用", 400);
            }

            if (!await _repo.DeviceExistsAsync(request.DeviceCode, ct))
            {
                throw new AppException("设备不存在或已停用", 400);
            }

            await _repo.ChangeAsync(
                request.Actor,
                request.DeviceCode,
                request.Tool,
                operatorName,
                grant,
                ct);
        }

        /// <summary>
        /// 试调用：参数校验 → 写 Started → 白名单/账号/授权检查 → 真实查询 → Succeeded。
        /// actor 只从 JWT 取，绝不作为工具参数传入。
        /// </summary>
        public async Task<McpReadResult> InvokeAsync(
            string actor,
            McpInvokeRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(actor))
            {
                // 正常请求已被 [Authorize] 拦在中间件，不会走到这里；
                // 这里不写 Started，避免伪造一条"已进入业务边界"的审计。
                throw new UnauthorizedAccessException("未认证");
            }

            var device = (request.DeviceCode ?? string.Empty).Trim();
            var tool = (request.Tool ?? string.Empty).Trim();
            var point = string.IsNullOrWhiteSpace(request.PointCode)
                ? "temperature"
                : request.PointCode.Trim();

            RequireLength(device, 50, "deviceCode");
            RequireLength(tool, 50, "tool");
            RequireLength(point, 80, "pointCode");

            var call = new McpCall
            {
                Id = Guid.NewGuid().ToString("N"),
                Actor = actor,
                DeviceCode = device,
                Tool = tool,
                // 只记录白名单参数，不记录任何凭据。
                ArgumentsJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["deviceCode"] = device,
                    ["tool"] = tool,
                    ["pointCode"] = point
                }),
                Status = "Started",
                ResultJson = string.Empty,
                StartedAt = DateTime.UtcNow
            };

            // 先写 Started 再查权限：被拒绝的越权尝试同样要落库。
            await _repo.AddCallAsync(call, ct);

            try
            {
                if (!Catalog.Any(x => x.Name == tool))
                {
                    throw new AppException("工具不在白名单内", 400);
                }

                if (!await _repo.ActorExistsAsync(actor, ct))
                {
                    throw new UnauthorizedAccessException("账号不存在或已停用");
                }

                if (!await _repo.AllowedAsync(actor, device, tool, ct))
                {
                    throw new UnauthorizedAccessException("未授予该设备工具权限");
                }

                var data = await ReadAsync(tool, device, point, ct);

                await _repo.CompleteAsync(
                    call.Id,
                    "Succeeded",
                    JsonSerializer.Serialize(data),
                    ct);

                return new McpReadResult(call.Id, "Succeeded", data);
            }
            catch (OperationCanceledException)
            {
                // 请求取消时用独立 5 秒超时写终态，不跟着请求一起被取消。
                await FinishAsync(call.Id, "Canceled", "请求已取消");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                await FinishAsync(call.Id, "Denied", ex.Message);
                throw new AppException(ex.Message, 403);
            }
            catch (AppException ex)
            {
                // 参数/白名单 400、设备不存在 404 等业务异常保持原状态码。
                await FinishAsync(call.Id, "Failed", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                await FinishAsync(call.Id, "Failed", ex.Message);

                // 对外不暴露内部异常细节，但要给出 callId 方便查审计。
                throw new AppException($"工具调用失败（callId={call.Id}）", 502);
            }
        }

        public Task<List<McpGrantChange>> ChangesAsync(CancellationToken ct) => _repo.ChangesAsync(ct);

        public Task<List<McpCall>> CallsAsync(CancellationToken ct) => _repo.CallsAsync(ct);

        public async Task<McpCall> CallAsync(string id, CancellationToken ct) =>
            await _repo.CallAsync(id, ct)
            ?? throw new AppException("调用记录不存在", 404);

        private Task<object> ReadAsync(
            string tool,
            string device,
            string point,
            CancellationToken ct) => tool switch
            {
                "get_device" => _query.DeviceAsync(device, ct),
                // 固定最近 1 分钟窗口，避免模型或客户端把范围拉到全表。
                "get_history" => _query.HistoryAsync(device, point, DateTime.UtcNow, ct),
                "get_alarms" => _query.AlarmsAsync(device, ct),
                _ => throw new AppException("工具不在白名单内", 400)
            };

        private async Task FinishAsync(string id, string status, string result)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await _repo.CompleteAsync(id, status, result, cts.Token);
            }
            catch
            {
                // 审计兜底失败不能覆盖原始异常：调用方更需要看到最初的原因。
            }
        }

        private static void RequireLength(string value, int max, string name)
        {
            if (value.Length == 0 || value.Length > max)
            {
                throw new AppException($"{name} 必须为 1~{max} 个字符", 400);
            }
        }
    }
}
