using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pomelo.EntityFrameworkCore.MySql.Query.Internal;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public class UsersController:ControllerBase
    {
        private readonly IAdminService _adminService;

        public UsersController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> GetById(long id)
        {
            if(id <= 0)
            {
                return BadRequest(new ApiResponse<AdminUserDto>
                {
                    Code = 400,
                    Message = "用户 ID 必须大于 0"
                });
            }

            var user = await _adminService.GetUserByIdAsync(id);

            if(user == null)
            {
                return NotFound(new ApiResponse<AdminUserDto>
                {
                    Code = 404,
                    Message = "用户不存在"
                });
            }

            return Ok(new ApiResponse<AdminUserDto>
            {
                Code = 200,
                Message = "获取用户成功",
                Data = user
            });
        }
    }
}
