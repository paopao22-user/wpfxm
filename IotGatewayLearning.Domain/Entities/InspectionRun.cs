using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// AI 设备巡检任务，对应 ai_inspection_runs 表。
    /// 一条记录 = 一个合并窗口内的一次巡检；排队、执行、重试、终态都在这一行。
    /// </summary>
    [Table("ai_inspection_runs")]
    public sealed class InspectionRun
    {
        // 32 位无连字符 GUID，便于放在 URL 与唯一索引里。
        [Key, Column("id"), MaxLength(32)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Column("plan_id")]
        public long PlanId { get; set; }

        // 执行身份，取自计划所有者；不是提交事件的人。
        [Column("owner"), MaxLength(50)]
        public string Owner { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Column("point_code"), MaxLength(80)]
        public string PointCode { get; set; } = string.Empty;

        [Column("question"), MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        // 计划 ID + 窗口桶；唯一索引负责窗口合并与并发裁决。
        [Column("merge_key"), MaxLength(120)]
        public string MergeKey { get; set; } = string.Empty;

        // Manual / Scheduled / Event
        [Column("source"), MaxLength(20)]
        public string Source { get; set; } = string.Empty;

        // Queued / Running / Retry / Succeeded / Failed / Canceled / Unknown
        [Column("state"), MaxLength(30)]
        public string State { get; set; } = "Queued";

        [Column("attempts")]
        public int Attempts { get; set; }

        // 首次执行冻结的证据快照；重试复用，不偷偷换成新数据。
        [Column("evidence_json", TypeName = "longtext")]
        public string EvidenceJson { get; set; } = string.Empty;

        [Column("answer", TypeName = "longtext")]
        public string Answer { get; set; } = string.Empty;

        // 可公开的错误提示，详细原因留在证据与服务端日志。
        [Column("error"), MaxLength(200)]
        public string Error { get; set; } = string.Empty;

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("next_at", TypeName = "datetime(6)")]
        public DateTime NextAt { get; set; } = DateTime.UtcNow;

        // 执行租约；过期说明 Worker 失联，置 Unknown 而不是自动再次消费模型额度。
        [Column("lease_until", TypeName = "datetime(6)")]
        public DateTime? LeaseUntil { get; set; }

        [Column("completed_at", TypeName = "datetime(6)")]
        public DateTime? CompletedAt { get; set; }
    }
}
