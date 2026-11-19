using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 控制申请操作审计，对应 control_audits 表。
    /// 只记录 Submit / Approve / Reject / Cancel，
    /// 不记录设备命令前后值，也不记录执行结果。
    /// </summary>
    [Table("control_audits")]
    public sealed class ControlAudit
    {
        [Key, Column("id")]
        public long Id { get; set; }

        [Column("request_id", TypeName = "char(32)"), MaxLength(32)]
        public string RequestId { get; set; } = string.Empty;

        [Column("actor"), MaxLength(50)]
        public string Actor { get; set; } = string.Empty;

        [Column("action"), MaxLength(40)]
        public string Action { get; set; } = string.Empty;

        [Column("remark"), MaxLength(500)]
        public string Remark { get; set; } = string.Empty;

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
