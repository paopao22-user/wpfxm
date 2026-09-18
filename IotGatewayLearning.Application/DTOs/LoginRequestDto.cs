using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    public class LoginRequestDto
    {
        // 用户名：设置默认值为 string.Empty，防止引用类型出现 null
        public string Username { get; set; } = string.Empty;

        // 密码：客户端传来的明文密码
        public string Password { get; set; } = string.Empty;
    }
}
