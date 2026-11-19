using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IotGatewayLearning.Domain.AI.Knowledge;

namespace IotGatewayLearning.Infrastructure.AI.Knowledge
{
    /// <summary>
    /// 关键词检索实现：不引入 Qdrant / Embedding。
    /// 真正的检索在 KnowledgeRepository 里用 MySQL LIKE 完成；
    /// 这里返回空 ID 列表，Repository 会自动走关键词分支。
    /// 将来接入向量库时，只需替换本类并让 SearchAsync 返回命中的 chunkId。
    /// </summary>
    public sealed class KeywordKnowledgeVectors : IKnowledgeVectors
    {
        public Task IndexAsync(
            string id,
            string owner,
            string model,
            string text,
            CancellationToken ct) => Task.CompletedTask;

        public Task<List<string>> SearchAsync(
            string owner,
            string model,
            string question,
            CancellationToken ct) => Task.FromResult(new List<string>());
    }
}
