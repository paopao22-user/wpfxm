using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.AI.Knowledge;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Domain.Repositories;
using IotGatewayLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IotGatewayLearning.Infrastructure.Repositories.Implementations
{
    /// <summary>
    /// 知识库数据访问：只负责取数与保存，不调用模型、不做业务判断。
    /// </summary>
    public sealed class KnowledgeRepository : IKnowledgeRepository
    {
        private readonly AppDbContext _db;

        public KnowledgeRepository(AppDbContext db) => _db = db;

        public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

        public Task<KnowledgeDocument?> FindAsync(long id, string actor, CancellationToken ct) =>
            _db.Set<KnowledgeDocument>()
                .SingleOrDefaultAsync(x => x.Id == id && x.Owner == actor, ct);

        public void AddDocument(KnowledgeDocument row) => _db.Set<KnowledgeDocument>().Add(row);

        public void AddChunks(IEnumerable<KnowledgeChunk> rows) => _db.Set<KnowledgeChunk>().AddRange(rows);

        public Task<List<KnowledgeDocument>> ListAsync(string actor, CancellationToken ct) =>
            _db.Set<KnowledgeDocument>().AsNoTracking()
                .Where(x => x.Owner == actor)
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync(ct);

        public async Task<List<KnowledgeCandidate>> CandidatesAsync(
            string actor,
            string model,
            string question,
            IReadOnlyList<string> ids,
            CancellationToken ct)
        {
            // 只有本人、同型号、Ready 的文档参与检索；退役与失败文档不进答案。
            var baseQuery =
                from chunk in _db.Set<KnowledgeChunk>().AsNoTracking()
                join doc in _db.Set<KnowledgeDocument>().AsNoTracking()
                    on chunk.DocumentId equals doc.Id
                where doc.Owner == actor && doc.ModelCode == model && doc.State == "Ready"
                select new { chunk, doc };

            var query = baseQuery;

            if (ids.Count > 0)
            {
                query = query.Where(x => ids.Contains(x.chunk.Id));
            }
            else
            {
                var terms = KnowledgeTerms.Split(question);
                if (terms.Count == 0)
                    return new List<KnowledgeCandidate>();

                // 逐词 LIKE 后 UNION 去重；每个词都是参数化查询，不拼字符串。
                var first = terms[0];
                query = baseQuery.Where(x => x.chunk.Text.Contains(first));

                foreach (var term in terms.Skip(1))
                {
                    var current = term;
                    query = query.Union(baseQuery.Where(x => x.chunk.Text.Contains(current)));
                }
            }

            var rows = await query
                .OrderBy(x => x.chunk.Id)
                .Take(20)
                .ToListAsync(ct);

            return rows
                .Select(x => new KnowledgeCandidate(
                    x.chunk.Id,
                    x.doc.Id,
                    x.doc.FileId,
                    x.doc.Revision,
                    x.chunk.Ordinal,
                    x.chunk.Text))
                .ToList();
        }

        public void AddAnswer(KnowledgeAnswer row) => _db.Set<KnowledgeAnswer>().Add(row);

        public Task<List<KnowledgeAnswer>> AnswersAsync(string actor, CancellationToken ct) =>
            _db.Set<KnowledgeAnswer>().AsNoTracking()
                .Where(x => x.Owner == actor)
                .OrderByDescending(x => x.Id)
                .Take(50)
                .ToListAsync(ct);
    }
}
