using IotGatewayLearning.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    public class UserDto
    {
        // 用户唯一标识
        public long Id { get; set; }

        // 登录账号
        public string Username { get; set; } = string.Empty;

        // 用户真实姓名（用于在 WPF 界面右上角展示：例如“欢迎，张三工程师”）
        public string RealName { get; set; } = string.Empty;

        // 角色名称（用于客户端根据角色判断界面按钮的显示/隐藏权限）
        public string Role { get; set; } = string.Empty;

        // 新 RBAC
        public List<string> Roles { get; set; } = new();

        public List<string> Permissions { get; set; } = new();

        public DateTime? LastLoginAt { get; set; }  
    }
}
