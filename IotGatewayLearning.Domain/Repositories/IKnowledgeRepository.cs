using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.AI.Knowledge;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Domain.Repositories
{
    // 知识库数据访问契约：只负责取数与保存，不做模型调用。
    public interface IKnowledgeRepository
    {
        Task SaveAsync(CancellationToken ct);

        Task<KnowledgeDocument?> FindAsync(long id, string actor, CancellationToken ct);

        void AddDocument(KnowledgeDocument row);

        void AddChunks(IEnumerable<KnowledgeChunk> rows);

        Task<List<KnowledgeDocument>> ListAsync(string actor, CancellationToken ct);

        // ids 非空时按切片 ID 取；为空时按问题关键词检索。
        Task<List<KnowledgeCandidate>> CandidatesAsync(
            string actor,
            string model,
            string question,
            IReadOnlyList<string> ids,
            CancellationToken ct);

        void AddAnswer(KnowledgeAnswer row);

        Task<List<KnowledgeAnswer>> AnswersAsync(string actor, CancellationToken ct);
    }
}
