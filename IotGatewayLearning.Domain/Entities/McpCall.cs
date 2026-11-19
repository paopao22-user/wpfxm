using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// 工具调用审计，对应 ai_mcp_calls 表。
    /// 关键顺序：先写 Started 再查权限。
    /// 反过来的话，被拒绝的越权请求不会留下任何痕迹。
    /// </summary>
    [Table("ai_mcp_calls")]
    public class McpCall
    {
        [Key, Column("id", TypeName = "char(32)"), MaxLength(32)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        // 调用者，来自 JWT，不接受客户端传参。
        [Column("actor"), MaxLength(50)]
        public string Actor { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Column("tool"), MaxLength(50)]
        public string Tool { get; set; } = string.Empty;

        // 只保存白名单允许的参数，不保存 JWT、连接串、模型密钥。
        [Column("arguments_json", TypeName = "json")]
        public string ArgumentsJson { get; set; } = "{}";

        // Started / Succeeded / Denied / Failed / Canceled
        [Column("status"), MaxLength(30)]
        public string Status { get; set; } = "Started";

        // 真实工具返回，不是模型描述。
        [Column("result_json", TypeName = "longtext")]
        public string ResultJson { get; set; } = string.Empty;

        [Column("started_at", TypeName = "datetime(6)")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at", TypeName = "datetime(6)")]
        public DateTime? CompletedAt { get; set; }
    }
}
