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
        // 核心解耦：专一依赖角色领域服务
        private readonly IRoleService _roleService;
        // 构造函数注入 IRoleService
        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
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
            var roles = await _roleService.GetRolesAsync(keyword);

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
            var role = await _roleService.GetRoleByIdAsync(id);

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
            var role = await _roleService.CreateRoleAsync(request);

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
            var role = await _roleService.UpdateRoleAsync(id, request);

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
            await _roleService.DeleteRoleAsync(id);

            // 2. 构造 200 成功响应模型返回
            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "删除角色成功",
                Data = null
            });
        }

        /// <summary>
        /// 查询指定角色已分配的权限 ID 列表
        /// </summary>
        /// <param name="id">角色主键 ID</param>
        /// <returns>已分配的权限 ID 数组</returns>
        [HttpGet("{id:long}/permissions")]
        public async Task<ActionResult<ApiResponse<List<long>>>> GetRolePermissions(long id)
        {
            // 1. 调用业务服务层执行查询与防线校验
            var permissions = await _roleService.GetRolePermissionIdsAsync(id);

            // 2. 包装为标准 200 OK 成功响应
            return Ok(new ApiResponse<List<long>>
            {
                Code = 200,
                Message = "查询分配权限信息成功",
                Data = permissions
            });
        }

        [HttpPut("{id:long}/permissions")]
        public async Task<ActionResult<ApiResponse<bool>>> SetRolePermissions(long id, [FromBody] AssignIdsRequest request)
        {
            // 1. 调度业务服务层执行四道安全防线、差集计算与单事务提交
            await _roleService.SetRolePermissionsAsync(id, request);

            // 2. 包装为标准化 200 OK 响应，Data 返回 true
            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "角色分配权限成功",
                Data = true
            });
        }
    }
}
