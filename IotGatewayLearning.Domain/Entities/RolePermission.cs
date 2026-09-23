using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    [Table("role_permissions")]
    public class RolePermission
    {
        // ==============================
        // 外键：角色
        // ==============================
        [Column("role_id")]
        public long RoleId { get; set; }

        // ==============================
        // 外键：权限
        // ==============================
        [Column("permission_id")]
        public long PermissionId { get; set; }

        // ==============================
        // 分配权限的时间
        // ==============================
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==============================
        // 导航属性
        // ==============================

        public Role Role { get; set; } = null!;

        public Permission Permission { get; set; } = null!;
    }
}
