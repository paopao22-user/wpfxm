using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    public class AdminUserDto
    {
        public long Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string RealName { get; set; } = string.Empty;

        public sbyte Status { get; set; }   
    }
}
