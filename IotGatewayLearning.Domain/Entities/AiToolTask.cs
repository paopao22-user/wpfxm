using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// AI 设备调查任务，对应 ai_tool_tasks 表。
    /// 一行 = 一次用户调查意图 + 一份真实工具执行记录 + 一次模型回答。
    /// </summary>
    [Table("ai_tool_tasks")]
    public class AiToolTask
    {
        [Key, Column("id")]
        public long Id { get; set; }

        // 客户端唯一意图编号；重发同一个键只回读，不再调用模型。
        [Column("request_key"), MaxLength(100)]
        public string RequestKey { get; set; } = string.Empty;

        // 从 JWT 取用户名；不能接受客户端指定别的用户。
        [Column("actor"), MaxLength(100)]
        public string Actor { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        // 项目现有 DeviceData/DevicePoint 的 point_code 都是 80。
        [Column("point_code"), MaxLength(80)]
        public string PointCode { get; set; } = string.Empty;

        // 用户问题只作为待回答的数据，不能覆盖工具权限。
        [Column("question"), MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        // Running / Succeeded / Failed / Canceled
        [Column("status"), MaxLength(30)]
        public string Status { get; set; } = "Running";

        // 模型最终文字；Succeeded 只表示拿到回答，不代表结论已确认。
        [Column("answer", TypeName = "mediumtext")]
        public string Answer { get; set; } = string.Empty;

        // 真实工具执行过程，不是模型推理文本。
        [Column("calls_json", TypeName = "json")]
        public string CallsJson { get; set; } = "[]";

        [Column("error_message"), MaxLength(300)]
        public string? ErrorMessage { get; set; }

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at", TypeName = "datetime(6)")]
        public DateTime? CompletedAt { get; set; }
    }
}
