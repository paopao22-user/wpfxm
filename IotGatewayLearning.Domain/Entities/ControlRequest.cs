using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备控制申请，对应 control_requests 表。
    /// 本表只保存"人想做什么、谁批的"，不保存设备命令执行结果。
    /// </summary>
    [Table("control_requests")]
    public sealed class ControlRequest
    {
        [Key, Column("id", TypeName = "char(32)"), MaxLength(32)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        // 客户端幂等键：32 位无横线 UUID，唯一索引裁决并发重复提交。
        [Column("request_key", TypeName = "char(32)"), MaxLength(32)]
        public string RequestKey { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        // setTemperature / powerOff
        [Column("action"), MaxLength(40)]
        public string Action { get; set; } = string.Empty;

        // 设温目标；关机为 null。
        [Column("target", TypeName = "decimal(5,1)")]
        public decimal? Target { get; set; }

        [Column("reason"), MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        // 申请人，只从 JWT 取，客户端不能提交。
        [Column("requester"), MaxLength(50)]
        public string Requester { get; set; } = string.Empty;

        // 审批人；Pending 时为空。
        [Column("reviewer"), MaxLength(50)]
        public string Reviewer { get; set; } = string.Empty;

        // Pending / Approved / Rejected / Canceled
        [Column("state"), MaxLength(30)]
        public string State { get; set; } = "Pending";

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 审批有效期：Pending 申请 30 分钟内允许审批。
        [Column("review_before", TypeName = "datetime(6)")]
        public DateTime ReviewBefore { get; set; } = DateTime.UtcNow.AddMinutes(30);

        // 执行有效期：批准后 5 分钟内允许开始执行；驳回为 null。
        [Column("execute_before", TypeName = "datetime(6)")]
        public DateTime? ExecuteBefore { get; set; }

        [Column("updated_at", TypeName = "datetime(6)")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
