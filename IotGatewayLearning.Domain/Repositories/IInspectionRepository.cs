using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    /// <summary>
    /// 巡检计划与任务队列的持久化入口。
    /// 抢占、租约回收、窗口合并都靠数据库条件更新和唯一索引裁决，
    /// 不靠进程内锁——多实例部署时进程内锁保护不了任何东西。
    /// </summary>
    public interface IInspectionRepository
    {
        Task<InspectionPlan?> PlanAsync(long id, CancellationToken ct);

        // 到期计划：Enabled && NextAt <= UtcNow，最多 20 条。
        Task<List<InspectionPlan>> DueAsync(CancellationToken ct);

        // 只把 NextAt 推到当前时刻 + 周期，不补历史漏掉的窗口。
        Task AdvanceAsync(InspectionPlan plan, CancellationToken ct);

        // 先回收过期租约，再条件抢占一条 Queued/Retry；抢不到返回 null。
        Task<InspectionRun?> ClaimAsync(CancellationToken ct);

        // 首次执行冻结证据，重试不再重新读取。
        Task EvidenceAsync(string id, string evidence, CancellationToken ct);

        // 只更新仍处于 Running 的行，防止覆盖被别人回收的任务。
        Task FinishAsync(InspectionRun row, CancellationToken ct);

        Task<InspectionPlan> AddPlanAsync(InspectionPlan plan, CancellationToken ct);

        Task<List<InspectionPlan>> PlansAsync(string owner, CancellationToken ct);

        // MergeKey 命中直接返回旧任务；并发插入 1062 冲突后重查。
        Task<InspectionRun> EnqueueAsync(InspectionRun run, CancellationToken ct);

        Task<bool> EnableAsync(long id, string owner, bool enabled, CancellationToken ct);

        Task<List<InspectionRun>> RunsAsync(string owner, CancellationToken ct);

        Task<InspectionRun?> RunAsync(string id, string owner, CancellationToken ct);
    }
}
