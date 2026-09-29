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
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
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

            var user = await _userService.GetUserByIdAsync(id);

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

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<AdminUserDto>>>> GetUsers([FromQuery]string? keyword)
        {
            var users = await _userService.GetUsersAsync(keyword);

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
            var user = await _userService.CreateUserAsync(request);

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
            var user = await _userService.UpdateUserAsync(id, request, actorId);

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
            

            await _userService.DeleteUserAsync(id, actorId);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "删除用户成功",
                Data = true
            });
        }

        /// <summary>
        /// 查询指定用户已分配的角色 ID 列表
        /// </summary>
        /// <param name="id">用户主键 ID</param>
        /// <returns>已分配的角色 ID 数组</returns>
        [HttpGet("{id:long}/roles")]
        public async Task<ActionResult<ApiResponse<List<long>>>> GetUserRoles(long id)
        {
            // 1. 调用业务服务层执行查询与防线校验
            var roleId = await _userService.GetUserRoleIdsAsync(id);

            // 2. 包装为标准 200 OK 成功响应
            return Ok(new ApiResponse<List<long>>
            {
                Code = 200,
                Message = "获取用户已分配角色成功",
                Data = roleId
            });
        }

        /// <summary>
        /// 为指定用户分配/替换角色集合（基于差集算法的完整替换模式）
        /// </summary>
        /// <param name="id">目标用户主键 ID</param>
        /// <param name="request">包含期望最终绑定的全部角色 ID 集合</param>
        /// <returns>操作成功标识</returns>
        [HttpPut("{id:long}/roles")]
        public async Task<ActionResult<ApiResponse<bool>>> SetUserRoles(long id, [FromBody] AssignIdsRequest request)
        {
            // 步骤 1：从当前经过 JWT 认证的 ClaimsPrincipal 中提取操作者 ID（用于防权限自死锁）
            var actorId = GetCurrentUserId();

            // 步骤 2：调度业务服务层执行四道安全防线、差集计算与单事务提交
            await _userService.SetUserRolesAsync(id, request, actorId);

            // 步骤 3：包装为标准化 200 OK 响应，Data 返回 true
            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "用户角色分配成功",
                Data = true
            });
        }



        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        /// <param name="page">页码（默认 1）</param>
        /// <param name="pageSize">每页大小（默认 10）</param>
        /// <param name="keyword">模糊搜索关键字（可选）</param>
        /// <param name="cancellationToken">异步取消令牌</param>
        /// <returns>标准化分页响应结果</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PageResult<AdminUserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            CancellationToken cancellationToken = default)
        {
            // 1. 调用业务服务层执行分页查询
            var pagedResult = await _userService.GetUserPageListAsync(page, pageSize, keyword, cancellationToken);

            // 2. 包装为 200 OK 标准响应格式返回
            return Ok(new ApiResponse<PageResult<AdminUserDto>>
            {
                Code = 200,
                Message = "获取用户分页列表成功",
                Data = pagedResult
            });
        }
    }
}
