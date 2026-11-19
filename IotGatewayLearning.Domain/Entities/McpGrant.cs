using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 设备工具授权，对应 ai_mcp_grants 表。
    /// 联合主键 Actor + DeviceCode + Tool：同一账号对同一设备的同一工具只有一条授权，
    /// 重复授予只更新时间和操作员，不会产生第二行。
    /// </summary>
    [Table("ai_mcp_grants")]
    public class McpGrant
    {
        // 被授权的账号（username），不是管理员。
        [Column("actor"), MaxLength(50)]
        public string Actor { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        // 只能是白名单里的 get_device / get_history / get_alarms。
        [Column("tool"), MaxLength(50)]
        public string Tool { get; set; } = string.Empty;

        // 执行这次授予的管理员账号。
        [Column("granted_by"), MaxLength(50)]
        public string GrantedBy { get; set; } = string.Empty;

        [Column("granted_at", TypeName = "datetime(6)")]
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}
