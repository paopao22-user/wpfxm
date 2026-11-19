using IotGatewayLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) :base(options)
        {
            
        }

        // ==============================
        // 实体集合
        // ==============================

        //用户表
        public DbSet<User> Users => Set<User>();

        //角色表
        public DbSet<Role> Roles => Set<Role>();

        //权限表
        public DbSet<Permission> Permissions => Set<Permission>();

        //用户角色表
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        //角色权限表
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        //设备表
        public DbSet<Device> Devices => Set<Device>();


        //设备点位表
        public DbSet<DevicePoint> DevicePoints => Set<DevicePoint>();

        //设备时序采样历史表
        public DbSet<DeviceData> DeviceData => Set<DeviceData>();

        //AI 设备分析记录表（Day44）
        public DbSet<AiAnalysisRecord> AiAnalysisRecords => Set<AiAnalysisRecord>();

        //AI 设备调查任务表（Day45）
        public DbSet<AiToolTask> AiToolTasks => Set<AiToolTask>();

        //AI 设备知识库文档、切片与回答（Day46）
        public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

        public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

        public DbSet<KnowledgeAnswer> KnowledgeAnswers => Set<KnowledgeAnswer>();

        //MCP 设备工具授权、调用审计与授权变更（Day47）
        public DbSet<McpGrant> McpGrants => Set<McpGrant>();

        public DbSet<McpCall> McpCalls => Set<McpCall>();

        public DbSet<McpGrantChange> McpGrantChanges => Set<McpGrantChange>();

        //AI 设备巡检计划与任务队列（Day48）
        public DbSet<InspectionPlan> InspectionPlans => Set<InspectionPlan>();

        public DbSet<InspectionRun> InspectionRuns => Set<InspectionRun>();

        //AI 告警研判与通知发件箱（Day49）
        public DbSet<AiAdvisory> AiAdvisories => Set<AiAdvisory>();

        public DbSet<AiNotification> AiNotifications => Set<AiNotification>();

        //设备控制申请与操作审计（Day50）
        public DbSet<ControlRequest> ControlRequests => Set<ControlRequest>();

        public DbSet<ControlAudit> ControlAudits => Set<ControlAudit>();

        /// <summary>
        /// 实体关系配置:正式告诉 EF Core 这些关系怎么连
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //base. : 表示 先调用父类 `DbContext` 原本的模型配置逻辑， 然后再执行你自己的配置。
            base.OnModelCreating(modelBuilder);

            // ==============================
            // 1. User 用户表配置
            // ==============================
            // 确保登录用户名在全局范围绝对唯一，防范并发注册碰撞与脏数据
            modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();


            // ==============================
            // Role
            // ==============================
            //整体理解：Role 表的 Code 字段建立唯一索引，不允许重复角色编码
            //modelBuilder.Entity<Role>():我要配置 Role 这个实体;
            //.HasIndex(x => x.Code): 给 Role.Code 建索引;
            //.IsUnique(): 这个索引必须唯一。
            modelBuilder.Entity<Role>().HasIndex(x => x.Code).IsUnique();


            // ==============================
            // Permission
            // ==============================
            //整体理解：Permission表的Code字段建立唯一索引，不允许重复权限编码
            //modelBuilder.Entity<Permission>(): 配置Permission这个实体类
            //.HasIndex(x => x.Code): 给Permission.Code 建索引
            //.IsUnique()： 索引必须唯一
            modelBuilder.Entity<Permission>().HasIndex(x => x.Code).IsUnique();

            

            // ==============================
            // UserRole
            // ==============================
            //复合主键

            modelBuilder.Entity<UserRole>().HasKey(x => new
            {
                x.UserId,
                x.RoleId
            });



            //modelBuilder.Entity<UserRole>()： 配置UserRole这个实体类
            //.HasOne(x => x.Role): 一个 UserRole 有一个 Role。
            //.WithMany(x => x.UserRoles): 一个Role对应多个UserRoles
            // .HasForeignKey(x => x.RoleId): 它们通过 UserRole.RoleId 外键关联
            //.OnDelete(DeleteBehavior.Cascade): 配置级联删除——当某个 Role（角色）被删除时，
            //自动删除 UserRole（中间表）里所有绑定了该 RoleId 的关联记录（只删关系，不删用户本人）。
            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);  //当主表 Role 中的某一行被删除时,自动级联删除 UserRole 表中所有 RoleId 等于该角色的记录

            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==============================
            // RolePermission
            // ==============================
            modelBuilder.Entity<RolePermission>().HasKey(x => new
            {
                x.RoleId,
                x.PermissionId
            });

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==============================
            //  Device 设备主表配置
            // ==============================
            // 业务设备编号全局唯一
            modelBuilder.Entity<Device>()
                .HasIndex(x => x.DeviceCode)
                .IsUnique();
            // 软删除全局查询过滤器：凡是针对 Device 的查询，默认自动追加 is_deleted = 0
            modelBuilder.Entity<Device>()
                .HasQueryFilter(x => x.IsDeleted == 0);
            // 一台设备拥有多个点位，点位通过 DeviceId 引用设备 (1:N)
            modelBuilder.Entity<Device>()
                .HasMany(x => x.Points)
                .WithOne(x => x.Device)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==============================
            //  DevicePoint 采集点位表配置
            // ==============================
            // 同一台设备内点位编码不能重复（device_id + point_code 联合唯一）
            modelBuilder.Entity<DevicePoint>()
                .HasIndex(x => new { x.DeviceId, x.PointCode })
                .IsUnique();


            // ==============================
            //  DeviceData 设备采样历史表配置
            // ==============================
            // 查询某台设备某个点位的最新值：
            // device_code + point_code 定位点位，occurred_at 倒序取第一条。
            modelBuilder.Entity<DeviceData>()
                .HasIndex(x => new { x.DeviceCode, x.PointCode, x.OccurredAt })
                .HasDatabaseName("ix_device_data_point_time")
                .IsDescending(false, false, true);

            // 查询某台设备某个时间范围的历史数据：
            // device_code 定位设备，occurred_at 支持时间范围过滤。
            modelBuilder.Entity<DeviceData>()
                .HasIndex(x => new { x.DeviceCode, x.OccurredAt })
                .HasDatabaseName("ix_device_data_device_time");


            // ==============================
            //  AiAnalysisRecord AI 分析记录表配置
            // ==============================
            // request_id 唯一索引：同一分析意图只允许一条记录，
            // 也是 TryAddAsync 在并发下判断"谁先占位"的数据库依据。
            modelBuilder.Entity<AiAnalysisRecord>()
                .HasIndex(x => x.RequestId)
                .IsUnique();

            // ==============================
            //  AiToolTask AI 设备调查任务表配置
            // ==============================
            // request_key 唯一索引：同一调查意图只允许一条记录，
            // 也是 AddAsync 在并发下判断“谁先占位”的数据库依据。
            modelBuilder.Entity<AiToolTask>()
                .HasIndex(x => x.RequestKey)
                .IsUnique();

            // ==============================
            //  KnowledgeDocument / KnowledgeChunk AI 知识库（Day46）
            // ==============================
            // 同一所有者、同一文件、同一版本只允许一条资料。
            modelBuilder.Entity<KnowledgeDocument>()
                .HasIndex(x => new { x.Owner, x.FileId, x.Revision })
                .IsUnique();

            // 按文档取切片时走 (document_id, ordinal) 索引，天然有序。
            modelBuilder.Entity<KnowledgeChunk>()
                .HasIndex(x => new { x.DocumentId, x.Ordinal });

            // ==============================
            //  McpGrant / McpCall / McpGrantChange MCP 工具权限（Day47）
            // ==============================
            // 一条授权 = 账号 + 设备 + 工具，三者联合主键。
            modelBuilder.Entity<McpGrant>()
                .HasKey(x => new { x.Actor, x.DeviceCode, x.Tool });

            // 按账号查最近调用记录。
            modelBuilder.Entity<McpCall>()
                .HasIndex(x => new { x.Actor, x.StartedAt })
                .HasDatabaseName("ix_mcp_actor_time");

            // ==============================
            //  InspectionPlan / InspectionRun 巡检计划与任务队列（Day48）
            // ==============================
            // 到期扫描走 (enabled, next_at)。
            modelBuilder.Entity<InspectionPlan>()
                .HasIndex(x => new { x.Enabled, x.NextAt })
                .HasDatabaseName("ix_inspection_plan_due");

            // merge_key 唯一索引：窗口合并与并发裁决都靠它。
            modelBuilder.Entity<InspectionRun>()
                .HasIndex(x => x.MergeKey)
                .IsUnique();

            // 抢占扫描走 (state, next_at)。
            modelBuilder.Entity<InspectionRun>()
                .HasIndex(x => new { x.State, x.NextAt })
                .HasDatabaseName("ix_inspection_ready");

            // 列表按 owner 过滤、created_at 倒序。
            modelBuilder.Entity<InspectionRun>()
                .HasIndex(x => new { x.Owner, x.CreatedAt })
                .HasDatabaseName("ix_inspection_owner");

            // ==============================
            //  AiAdvisory / AiNotification 告警研判与通知（Day49）
            // ==============================
            // 同一份巡检证据只研判一次。
            modelBuilder.Entity<AiAdvisory>()
                .HasIndex(x => x.RunId)
                .IsUnique();

            // 每条研判至多一条通知待办。
            modelBuilder.Entity<AiNotification>()
                .HasIndex(x => x.AdvisoryId)
                .IsUnique();

            // ==============================
            //  ControlRequest / ControlAudit 控制申请与审计（Day50）
            // ==============================
            // 客户端幂等键全局唯一，并发重复提交由数据库裁决。
            modelBuilder.Entity<ControlRequest>()
                .HasIndex(x => x.RequestKey)
                .IsUnique();

            // 设温目标最大 9999.9，保留一位小数，与 DECIMAL(5,1) 对齐。
            modelBuilder.Entity<ControlRequest>()
                .Property(x => x.Target)
                .HasPrecision(5, 1);

            // 审计行通过 request_id 外键指向申请；配置关系后 EF 才保证
            // 同一次 SaveChanges 里先插申请、再插审计，否则触发外键约束。
            modelBuilder.Entity<ControlAudit>()
                .HasOne<ControlRequest>()
                .WithMany()
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
