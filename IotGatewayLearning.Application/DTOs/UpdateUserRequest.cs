using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 修改用户资料请求参数 DTO
    /// </summary>
    public class UpdateUserRequest
    {
        public string RealName { get; set; } = string.Empty;

        public sbyte Status { get; set; } = 1;
    }
}
