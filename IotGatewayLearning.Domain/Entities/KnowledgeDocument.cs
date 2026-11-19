using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备知识库资料，对应 ai_knowledge_documents 表。
    /// 一行 = 一个所有者导入的一份设备资料的一个版本。
    /// </summary>
    [Table("ai_knowledge_documents")]
    public sealed class KnowledgeDocument
    {
        [Key, Column("id")]
        public long Id { get; set; }

        // 上传人，只从 JWT 取。
        [Column("owner"), MaxLength(100)]
        public string Owner { get; set; } = string.Empty;

        [Column("device_id")]
        public long DeviceId { get; set; }

        // 设备型号，检索时按型号过滤，避免把别的设备资料混进来。
        [Column("model_code"), MaxLength(80)]
        public string ModelCode { get; set; } = string.Empty;

        [Column("file_id")]
        public long FileId { get; set; }

        [Column("revision"), MaxLength(40)]
        public string Revision { get; set; } = string.Empty;

        // Parsing / Indexing / Ready / NeedsOcr / Failed / Retired
        [Column("state"), MaxLength(30)]
        public string State { get; set; } = "Parsing";

        // 失败原因，可公开；详细异常只进服务端日志。
        [Column("error"), MaxLength(300)]
        public string? Error { get; set; }

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
