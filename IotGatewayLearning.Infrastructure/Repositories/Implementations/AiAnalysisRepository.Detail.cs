using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IotGatewayLearning.Infrastructure.Repositories.Implementations
{
    public sealed partial class AiAnalysisRepository
    {
        // 同时匹配 id 和创建者，不能只凭前端隐藏列表来限制访问。
        public Task<AiAnalysisRecord?> FindAsync(
            long id,
            string actor,
            CancellationToken ct) =>
            _db.Set<AiAnalysisRecord>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.CreatedBy == actor, ct);
    }
}
