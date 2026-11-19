using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotGatewayLearning.Domain.Entities
{
    /// <summary>
    /// AI 设备巡检计划，对应 ai_inspection_plans 表。
    /// 一行 = 一个所有者对一台设备一个点位的周期巡检配置。
    /// </summary>
    [Table("ai_inspection_plans")]
    public sealed class InspectionPlan
    {
        [Key, Column("id")]
        public long Id { get; set; }

        // 计划所有者；定时与事件触发的执行身份仍取该值，事件入口不能冒充其他用户。
        [Column("owner"), MaxLength(50)]
        public string Owner { get; set; } = string.Empty;

        [Column("device_code"), MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Column("point_code"), MaxLength(80)]
        public string PointCode { get; set; } = string.Empty;

        // 巡检问题；任务创建时复制，后续修改计划不改变旧任务。
        [Column("question"), MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        // 巡检周期，单位秒；创建时限制 60-86400。
        [Column("interval_seconds")]
        public int IntervalSeconds { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; } = true;

        // 下次调度时间，不是设备最近采样时间。
        [Column("next_at", TypeName = "datetime(6)")]
        public DateTime NextAt { get; set; } = DateTime.UtcNow;
    }
}
