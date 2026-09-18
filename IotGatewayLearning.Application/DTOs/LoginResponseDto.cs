using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    public class LoginResponseDto
    {
        // 核心凭据：生成的身份验证令牌（如 JWT，JSON Web Token）
        public string Token { get; set; } = string.Empty;

        // 令牌过期的绝对时间戳（以秒或毫秒为单位）
        public long ExpiresAt { get; set; }

        // 复合属性：嵌套携带当前登录用户的精简信息
        public UserDto User { get; set; } = new();
    }
}
