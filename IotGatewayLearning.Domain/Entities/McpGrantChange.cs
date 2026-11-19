using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 授权变更记录，对应 ai_mcp_grant_changes 表。
    /// 与 ai_mcp_grants 分离：授权表只保留"当前状态"，
    /// 变更表保留"历史上谁改过什么"。
    /// </summary>
    [Table("ai_mcp_grant_changes")]
    public class McpGrantChange
    {
        [Key, Column("id")]
        public long Id { get; set; }

        // 执行操作的管理员。
        [Column("operator"), MaxLength(50)]
        public string Operator { get; set; } = string.Empty;

        // 被授权/被撤销的账号。
        [Column("actor"), MaxLength(50)]
        public string Actor { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Column("tool"), MaxLength(50)]
        public string Tool { get; set; } = string.Empty;

        // Grant / Revoke
        [Column("action"), MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        [Column("occurred_at", TypeName = "datetime(6)")]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
