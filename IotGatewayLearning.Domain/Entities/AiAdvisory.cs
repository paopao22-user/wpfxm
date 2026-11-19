using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// AI 告警研判记录，对应 ai_advisories 表。
    /// 一行 = 一次巡检证据的模型研判结论；不能替代 Day31 的规则告警。
    /// </summary>
    [Table("ai_advisories")]
    public sealed class AiAdvisory
    {
        [Key, Column("id"), MaxLength(32)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        // 来源巡检任务；唯一索引保证同一巡检只研判一次。
        [Column("run_id"), MaxLength(32)]
        public string RunId { get; set; } = string.Empty;

        [Column("owner"), MaxLength(50)]
        public string Owner { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        // Running / Active / NoAttention / Failed / Acknowledged / Closed
        [Column("state"), MaxLength(30)]
        public string State { get; set; } = "Running";

        // 研判使用的真实证据快照，不从模型文字反推。
        [Column("evidence_json", TypeName = "longtext")]
        public string EvidenceJson { get; set; } = string.Empty;

        [Column("summary"), MaxLength(1000)]
        public string Summary { get; set; } = string.Empty;

        [Column("advice"), MaxLength(2000)]
        public string Advice { get; set; } = string.Empty;

        [Column("remark"), MaxLength(500)]
        public string Remark { get; set; } = string.Empty;

        [Column("handled_by"), MaxLength(50)]
        public string HandledBy { get; set; } = string.Empty;

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at", TypeName = "datetime(6)")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
