using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.Entities;

namespace IotGatewayLearning.Application.AI.Knowledge
{
    public interface IKnowledgeService
    {
        Task<KnowledgeDocument> ImportAsync(ImportKnowledge r, string actor, CancellationToken ct);

        Task<List<KnowledgeDocument>> ListAsync(string actor, CancellationToken ct);

        Task<KnowledgeAnswer> AskAsync(AskKnowledge r, string actor, CancellationToken ct);

        Task<List<KnowledgeAnswer>> AnswersAsync(string actor, CancellationToken ct);

        Task<KnowledgeDocument> RetireAsync(long id, string actor, CancellationToken ct);
    }
}
