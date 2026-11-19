using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace IotGatewayLearning.Infrastructure.AI.Tasks
{
    public sealed class AiTaskRepository : IAiTaskRepository
    {
        private readonly AppDbContext _db;

        public AiTaskRepository(AppDbContext db) => _db = db;

        public Task<AiToolTask?> ByKeyAsync(string key, CancellationToken ct) =>
            _db.Set<AiToolTask>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestKey == key, ct);

        public async Task<bool> AddAsync(AiToolTask task, CancellationToken ct)
        {
            _db.Set<AiToolTask>().Add(task);            // ① INSERT 一行 Running（唯一索引兜并发）

            try
            {
                await _db.SaveChangesAsync(ct);         // ② SaveChangesAsync → 成功就保存
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                // 并发重复请求由唯一索引判定，不重试模型调用。
                _db.Entry(task).State = EntityState.Detached;
                return false;
            }
        }

        public async Task SaveAsync(CancellationToken ct) => await _db.SaveChangesAsync(ct);

        public Task<List<AiToolTask>> ListAsync(string actor, CancellationToken ct) =>
            _db.Set<AiToolTask>().AsNoTracking()
                .Where(x => x.Actor == actor)
                .OrderByDescending(x => x.Id)
                .Take(50)
                .ToListAsync(ct);

        public Task<AiToolTask?> GetAsync(long id, string actor, CancellationToken ct) =>
            _db.Set<AiToolTask>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.Actor == actor, ct);
    }
}
