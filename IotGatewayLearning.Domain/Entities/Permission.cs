using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    [Table("permissions")]
    public class Permission
    {
        // ==============================
        // 主键
        // ==============================
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // ==============================
        // 权限编码
        // 例如：device:read
        // ==============================
        [Required]
        [MaxLength(100)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        // ==============================
        // 权限名称
        // ==============================
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        // ==============================
        // 权限说明
        // ==============================
        [MaxLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        // ==============================
        // 状态
        // ==============================
        [Column("status")]
        public sbyte Status { get; set; } = 1;

        // ==============================
        // 导航属性
        // 一个权限可以属于多个角色
        // ==============================
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
