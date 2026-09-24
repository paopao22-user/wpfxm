using IotGatewayLearning.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminUserDto?> GetUserByIdAsync(long targetUserId);
    }
}
