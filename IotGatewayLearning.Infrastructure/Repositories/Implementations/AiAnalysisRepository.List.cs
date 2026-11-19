using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IotGatewayLearning.Infrastructure.Repositories.Implementations
{
    public sealed partial class AiAnalysisRepository
    {
        public Task<List<AiAnalysisRecord>> ListAsync(
            string actor,
            string device,
            long? beforeId,
            CancellationToken ct) =>
            _db.Set<AiAnalysisRecord>()
                .AsNoTracking()
                // 管理员也只查询自己发起的分析，设备范围仍由服务端约束。
                .Where(x => x.CreatedBy == actor && x.DeviceCode == device
                    && (!beforeId.HasValue || x.Id < beforeId.Value))
                .OrderByDescending(x => x.Id)
                .Take(21)
                .ToListAsync(ct);
    }
}
