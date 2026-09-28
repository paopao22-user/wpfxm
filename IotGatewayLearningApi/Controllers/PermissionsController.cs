using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize(Policy = PermissionCodes.UserManage)]
    public class PermissionsController: ControllerBase
    {
        private readonly IAdminService _adminService;

        public PermissionsController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AdminPermissionDto>>>> GetPermissions([FromQuery] string? keyword)
        {
            // 1. 调度业务服务层查询数据
            var permissions = await _adminService.GetPermissionsAsync(keyword);

            // 2. 包装为统一成功的 ApiResponse 响应模型返回
            return Ok(new ApiResponse<List<AdminPermissionDto>>
            {
                Code = 200,
                Message = "查询权限信息成功",
                Data = permissions
            });
        }

        [HttpGet("{id:long}")]// 明确路由占位符并严格限定为 long 类型整数
        public async Task<ActionResult<ApiResponse<AdminPermissionDto>>> GetById(long id)
        {
            // 步骤 1：防御性校验，数据库自增主键必然从 1 开始，<= 0 属于客户端非法传参
            if(id <= 0)
            {
                return BadRequest(new ApiResponse<AdminPermissionDto>
                {
                    Code = 400,
                    Message = "权限id必须大于0"
                });
            }

            // 步骤 2：调度应用服务层执行查询
            var permission = await _adminService.GetPermissionByIdAsync(id);

            // 步骤 3：如果数据库未查询到该实体，按照 RESTful 规范返回 404
            if(permission == null)
            {
                return NotFound(new ApiResponse<AdminPermissionDto>
                {
                    Code = 404,
                    Message = "权限不存在"
                });
            }

            // 步骤 4：查到实体，返回 200 成功响应并携带 DTO 数据
            return Ok(new ApiResponse<AdminPermissionDto>
            {
                Code = 200,
                Message = "获取权限详情成功",
                Data = permission
            });
        }

        /// <summary>
        /// 新增权限
        /// </summary>
        /// <param name="request">保存权限请求模型</param>
        /// <returns>新增成功后的权限详细信息</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AdminPermissionDto>>> Create([FromBody]SavePermissionRequest request)
        {
            // 1. 调度业务服务层执行创建
            var permission = await _adminService.CreatePermissionAsync(request);

            // 2. 返回 200 成功响应并携带新生成的实体数据（含自增 ID）
            return Ok(new ApiResponse<AdminPermissionDto>
            {
                Code = 200,
                Message = "新增权限成功",
                Data = permission
            });
        }

        /// <summary>
        /// 修改指定权限信息
        /// </summary>
        /// <param name="id">路由占位符：目标权限主键 ID</param>
        /// <param name="request">请求体 JSON：包含更新后的数据</param>
        /// <returns>更新成功后的权限详情</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<AdminPermissionDto>>> Update(long id, [FromBody]SavePermissionRequest request)
        {
            // 1. 调度业务服务层执行修改
            var permission = await _adminService.UpdatePermissionAsync(id, request);

            // 2. 包装为标准 200 OK 响应返回
            return Ok(new ApiResponse<AdminPermissionDto>
            {
                Code = 200,
                Message = "修改权限信息成功",
                Data = permission
            });
        }

        /// <summary>
        /// 删除指定权限（软删除）
        /// </summary>
        /// <param name="id">权限主键 ID</param>
        /// <returns>标准化 200 操作结果</returns>
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
        {
            await _adminService.DeletePermissionAsync(id);


            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "软删除权限成功",
                Data = null
            });
        }


        /// <summary>
        /// 获取层级权限树（供前端角色授权复选框树组件渲染）
        /// </summary>
        /// <returns>标准的层级树形 JSON 列表</returns>
        [HttpGet("tree")] // 显式声明子路由为 tree，完整路径：GET /api/permissions/tree
        public async Task<ActionResult<ApiResponse<List<PermissionNodeDto>>>> GetTree()
        {
            // 1. 调度业务服务层生成层级树
            var tree = await _adminService.GetPermissionTreeAsync();

            // 2. 包装为 200 成功响应返回
            return Ok(new ApiResponse<List<PermissionNodeDto>>
            {
                Code = 200,
                Message = "获取权限树成功",
                Data = tree
            });
        }
    }
}
