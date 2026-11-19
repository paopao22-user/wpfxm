using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 通知事务发件箱，对应 ai_notification_outbox 表。
    /// 与研判在同一个 SaveChanges 中提交，后台 Worker 从已提交待办恢复发送。
    /// </summary>
    [Table("ai_notification_outbox")]
    public sealed class AiNotification
    {
        [Key, Column("id"), MaxLength(32)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        // 每条研判至多一条待办，由唯一索引保证。
        [Column("advisory_id"), MaxLength(32)]
        public string AdvisoryId { get; set; } = string.Empty;

        // Pending / Sending / Retry / Accepted / Failed / Unknown
        [Column("state"), MaxLength(30)]
        public string State { get; set; } = "Pending";

        [Column("attempts")]
        public int Attempts { get; set; }

        [Column("next_at", TypeName = "datetime(6)")]
        public DateTime NextAt { get; set; } = DateTime.UtcNow;

        // 只保留可公开的失败分类，不写第三方原始响应。
        [Column("last_error"), MaxLength(200)]
        public string LastError { get; set; } = string.Empty;

        [Column("updated_at", TypeName = "datetime(6)")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
