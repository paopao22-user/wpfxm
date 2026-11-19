using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.AI.Knowledge
{
    // 引用快照固定 6 个字段，调用模型前序列化落库；模型正文里的编号不作为权威引用。
    public sealed record KnowledgeCitation(
        string ChunkId,
        long DocumentId,
        long FileId,
        string Revision,
        int Ordinal,
        string Text);

    // 检索候选：带文档元数据，Service 可以直接生成引用而不必再查一次库。
    public sealed record KnowledgeCandidate(
        string ChunkId,
        long DocumentId,
        long FileId,
        string Revision,
        int Ordinal,
        string Text);

    // 原始文件内容与类型；不允许模型直接访问任意 URL。
    public sealed record KnowledgeSource(byte[] Bytes, string ContentType);

    // 文件来源：按 fileId 读取原始内容。
    public interface IKnowledgeSource
    {
        Task<KnowledgeSource> ReadAsync(
            long fileId,
            long deviceId,
            string actor,
            CancellationToken ct);
    }

    // 检索索引契约：当前用 MySQL 关键词实现，将来换向量库时只替换实现。
    public interface IKnowledgeVectors
    {
        Task IndexAsync(string id, string owner, string model, string text, CancellationToken ct);

        Task<List<string>> SearchAsync(string owner, string model, string question, CancellationToken ct);
    }
}
