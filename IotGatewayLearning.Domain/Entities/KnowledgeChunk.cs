using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备知识库切片，对应 ai_knowledge_chunks 表。
    /// 切片是检索与引用的最小单位，Ordinal 从 1 开始。
    /// </summary>
    [Table("ai_knowledge_chunks")]
    public sealed class KnowledgeChunk
    {
        [Key, Column("id"), MaxLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString("D");

        [Column("document_id")]
        public long DocumentId { get; set; }

        [Column("ordinal")]
        public int Ordinal { get; set; }

        [Column("text", TypeName = "longtext")]
        public string Text { get; set; } = string.Empty;
    }
}
