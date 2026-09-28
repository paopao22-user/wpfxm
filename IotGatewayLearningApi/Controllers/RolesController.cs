using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public class RolesController:ControllerBase
    {
        private readonly IAdminService _adminService;

        public RolesController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// 获取角色列表
        /// </summary>
        /// <param name="keyword">查询关键字</param>
        /// <returns>返回AdminRoleDto的列表集合</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AdminRoleDto>>>> GetRoles([FromQuery]string? keyword)
        {
            // 1. 调度业务服务层获取数据
            var roles = await _adminService.GetRolesAsync(keyword);

            // 2. 统一封装为 200 成功响应模型返回
            return Ok(new ApiResponse<List<AdminRoleDto>>
            {
                Code = 200,

                Message = "获取角色列表成功",

                Data = roles
            });
        }

        /// <summary>
        /// 根据角色主键 ID 查询角色详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:long}")]  //明确路由占位符并约束为 long 类型
        public async Task<ActionResult<ApiResponse<AdminRoleDto>>> GetById(long id)
        {
            //防御性校验，数据库自增主键必然从 1 开始，<= 0 属于客户端非法传参
            if(id <= 0)
            {
                return BadRequest(new ApiResponse<AdminRoleDto>
                {
                    Code = 400,

                    Message = "角色id必须大于0"
                });
            }

            //调度应用服务层执行查询
            var role = await _adminService.GetRoleByIdAsync(id);

            //如果角色没有查询到
            if(role == null)
            {
                return NotFound(new ApiResponse<AdminRoleDto>
                {
                    Code = 404,

                    Message = "角色不存在"
                });
            }

            return Ok(new ApiResponse<AdminRoleDto>
            {
                Code = 200,

                Message = "获取角色详情成功",

                Data = role
            });
        }

        /// <summary>
        /// 新增角色
        /// </summary>
        /// <param name="request">保存角色请求模型</param>
        /// <returns>新增成功后的角色信息</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AdminRoleDto>>> Create([FromBody] SaveRoleRequest request)
        {
            // 1. 调度业务服务层执行创建
            var role = await _adminService.CreateRoleAsync(request);

            // 2. 返回 200 成功响应并携带新实体数据
            return Ok(new ApiResponse<AdminRoleDto>
            {
                Code = 200,

                Message = "新增角色成功",

                Data = role
            });
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<AdminRoleDto>>> Update(long id, [FromBody]SaveRoleRequest request)
        {
            // 1. 调度业务服务层执行修改
            var role = await _adminService.UpdateRoleAsync(id, request);

            return Ok(new ApiResponse<AdminRoleDto>
            {
                Code = 200,

                Message = "修改角色成功",

                Data = role
            });
        }

        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
        {
            // 1. 调度业务服务层执行软删除
            await _adminService.DeleteRoleAsync(id);

            // 2. 构造 200 成功响应模型返回
            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "删除角色成功",
                Data = null
            });
        }
    }
}
