using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
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
                // 防御性校验：数据库主键自增 ID 必然从 1 开始，<= 0 属于非法客户端传参
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

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AdminUserDto>>>> GetUsers([FromQuery]string? keyword)
        {
            var users = await _adminService.GetUsersAsync(keyword);

            return Ok(new ApiResponse<List<AdminUserDto>>
            {
                Code = 200,

                Message = "获取用户列表成功",

                Data = users
            });
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> Create([FromBody]CreateUserRequest request)
        {
            // 1. 调用业务服务完成创建流程
            var user = await _adminService.CreateUserAsync(request);

            // 2. 返回 200 成功响应并携带新创建的用户信息
            return Ok(new ApiResponse<AdminUserDto>
            {
                Code = 200,

                Message = "新增用户成功",

                Data = user
            });
        }

        /// <summary>
        /// 从当前经过 JWT 认证的 ClaimsPrincipal 中安全提取当前登录者 UserId
        /// </summary>
        private long GetCurrentUserId()
        {
            var userIdText = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if(!long.TryParse(userIdText, out var userId) || userId <= 0)
            {
                throw new UnauthorizedAppException("无效的用户身份认证信息");
            }

            return userId;
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> Update(long id, [FromBody] UpdateUserRequest request)
        {
            // 1. 获取当前登录者 ID 并调用服务层
            var actorId = GetCurrentUserId();
            var user = await _adminService.UpdateUserAsync(id, request, actorId);

            // 2. 返回 200 响应
            return Ok(new ApiResponse<AdminUserDto>
            {
                Code = 200,
                Message = "修改用户成功",
                Data = user
            });
        }

        [HttpDelete("{id:long}")]
        
        public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
        {
            // 步骤 1：从当前登录 JWT 的 Claims 中提取操作者 ID
            var actorId = GetCurrentUserId();
            

            await _adminService.DeleteUserAsync(id, actorId);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "删除用户成功",
                Data = true
            });
        }
    }
}
