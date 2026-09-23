using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    [Table("user_roles")]
    public class UserRole
    {
        // ==============================
        // 外键：用户
        // ==============================
        [Column("user_id")]
        public long UserId { get; set; }

        // ==============================
        // 外键：角色
        // ==============================
        [Column("role_id")]
        public long RoleId { get; set; }

        // ==============================
        // 分配角色的时间
        // ==============================
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==============================
        // 导航属性：当前关系属于哪个用户
        // ==============================
        public User User { get; set; } = null!;

        // ==============================
        // 导航属性：当前关系属于哪个角色
        // ==============================
        public Role Role { get; set; } = null!;
    }
}
