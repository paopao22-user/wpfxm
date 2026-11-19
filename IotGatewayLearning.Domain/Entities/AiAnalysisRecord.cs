using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// AI 设备分析记录，对应 ai_analysis_records 表。
    /// 一行 = 一次用户发起的分析意图 + 一份冻结的证据快照 + 一次执行结果。
    /// </summary>
    [Table("ai_analysis_records")]
    public class AiAnalysisRecord
    {
        [Key, Column("id")]
        public long Id { get; set; }

        // 客户端为一次用户意图生成的 UUID；网络重发时保持不变。
        [Column("request_id"), MaxLength(36)]
        public string RequestId { get; set; } = string.Empty;

        // 从 JWT 取用户名，不能接受客户端指定其他用户。
        [Column("created_by"), MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        // 你项目 DeviceData/DevicePoint 的 point_code 都是 80。
        [Column("point_code"), MaxLength(80)]
        public string PointCode { get; set; } = string.Empty;

        [Column("window_minutes")]
        public int WindowMinutes { get; set; }

        [Column("window_start", TypeName = "datetime(6)")]
        public DateTime WindowStart { get; set; }

        [Column("window_end", TypeName = "datetime(6)")]
        public DateTime WindowEnd { get; set; }

        // 本次真实输入快照；后续采样入库不能改写它。
        [Column("input_json", TypeName = "json")]
        public string InputJson { get; set; } = "{}";

        [Column("prompt_version"), MaxLength(30)]
        public string PromptVersion { get; set; } = string.Empty;

        // 本次实际使用的应用指令；改配置不改旧记录。
        [Column("system_prompt", TypeName = "text")]
        public string SystemPrompt { get; set; } = string.Empty;

        // 服务端配置的模型标识，不是 Agent 的 Name。
        [Column("model_id"), MaxLength(100)]
        public string ModelId { get; set; } = string.Empty;

        // Running / Succeeded / InsufficientData / Failed / Canceled
        [Column("status"), MaxLength(30)]
        public string Status { get; set; } = "Running";

        // 是否进入模型请求阶段；不证明厂商已计费。
        [Column("model_attempted")]
        public bool ModelAttempted { get; set; }

        // 模型最终文字；失败或未调用模型时为空字符串。
        [Column("answer", TypeName = "mediumtext")]
        public string Answer { get; set; } = string.Empty;

        [Column("error_code"), MaxLength(40)]
        public string? ErrorCode { get; set; }

        [Column("error_message"), MaxLength(500)]
        public string? ErrorMessage { get; set; }

        // null 表示厂商未提供，不代表 0。
        [Column("input_tokens")]
        public long? InputTokens { get; set; }

        [Column("output_tokens")]
        public long? OutputTokens { get; set; }

        [Column("elapsed_ms")]
        public long ElapsedMs { get; set; }

        [Column("created_at", TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; }

        [Column("completed_at", TypeName = "datetime(6)")]
        public DateTime? CompletedAt { get; set; }
    }
}
