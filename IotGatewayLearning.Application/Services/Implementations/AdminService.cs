using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 根据id查询用户
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        public async Task<AdminUserDto?> GetUserByIdAsync(long targetUserId)
        {
            //调用的是现有泛型仓储的按 ID 查询用户
            var user = await _unitOfWork.Users.GetByIdAsync(targetUserId);

            if(user == null)
            {
                return null;
            }

            return new AdminUserDto
            {
                Id = user.Id,

                Username = user.Username,

                RealName = user.RealName,

                Status = user.Status
            };
        }
    }
}
