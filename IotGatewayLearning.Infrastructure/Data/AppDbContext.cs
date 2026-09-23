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

        /// <summary>
        /// 实体关系配置:正式告诉 EF Core 这些关系怎么连
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //base. : 表示 先调用父类 `DbContext` 原本的模型配置逻辑， 然后再执行你自己的配置。
            base.OnModelCreating(modelBuilder);

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

        }
    }
}
