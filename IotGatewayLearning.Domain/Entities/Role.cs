using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    [Table("roles")]
    public class Role
    {
        // ==============================
        // 主键
        // ==============================
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // ==============================
        // 角色编码
        // ==============================
        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        // ==============================
        // 角色名称
        // ==============================
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;


        // ==============================
        // 状态
        // 1 = 启用
        // 0 = 停用
        // ==============================
        [Column("status")]
        public sbyte Status { get; set; } = 1;

        // ==============================
        // 创建时间
        // ==============================
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==============================
        // 导航属性
        // 一个Role可以分配给多个User
        // ==============================
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // ==============================
        // 导航属性
        // 一个Role可以拥有多个Permission
        // ==============================
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
