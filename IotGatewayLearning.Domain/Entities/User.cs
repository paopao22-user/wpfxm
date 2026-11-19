using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Domain.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("real_name")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户头像。
        /// ⚠️ 字段名沿用数据库列名 avatar_url，但里面存的是【OSS 的对象名(objectKey)】，
        ///    例如 avatars/8f3a1c2b....jpg，不是能直接点开的 URL。
        ///    原因：Bucket 是私有的，可访问的是"临时签名 URL"、会过期；
        ///          存 URL 的话，过一段时间头像就打不开了。
        ///    要显示时由 IOssFileStorage.BuildSignedUrl 现签一个 URL。
        /// </summary>
        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [Column("status")]
        public sbyte Status { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }


        // ==============================
        // RBAC导航属性
        // 一个用户可以拥有多个角色
        // ==============================
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
