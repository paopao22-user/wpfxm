using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备知识库回答，对应 ai_knowledge_answers 表。
    /// CitationsJson 是后端实际检索结果的权威快照，不是模型正文里的编号。
    /// </summary>
    [Table("ai_knowledge_answers")]
    public sealed class KnowledgeAnswer
    {
        [Key, Column("id")]
        public long Id { get; set; }

        [Column("owner"), MaxLength(100)]
        public string Owner { get; set; } = string.Empty;

        [Column("model_code"), MaxLength(80)]
        public string ModelCode { get; set; } = string.Empty;

        [Column("question"), MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        [Column("answer", TypeName = "mediumtext")]
        public string Answer { get; set; } = string.Empty;

        // NoEvidence / Running / Succeeded / Failed
        [Column("state"), MaxLength(30)]
        public string State { get; set; } = string.Empty;

        // 调用模型前序列化的引用快照，格式固定 6 个字段。
        [Column("citations_json", TypeName = "json")]
        public string CitationsJson { get; set; } = "[]";

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
