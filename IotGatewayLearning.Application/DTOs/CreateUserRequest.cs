using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 新增用户请求参数 DTO
    /// </summary>
    public class CreateUserRequest
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RealName { get; set; } = string.Empty;    

        [Range(0,1)]
        public sbyte Status { get; set; } = 1;

    }
}
