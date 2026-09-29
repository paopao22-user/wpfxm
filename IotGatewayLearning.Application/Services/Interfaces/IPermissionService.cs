using IotGatewayLearning.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 权限领域业务服务契约接口
    /// 涵盖权限元数据的维护、内置安全红线防御以及层级权限树的分组组装
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 获取权限列表（支持根据关键字对 Code 和 Name 进行模糊匹配）
        /// </summary>
        Task<List<AdminPermissionDto>> GetPermissionsAsync(string? keyword);

        /// <summary>
        /// 通过权限主键 ID 获取权限详情
        /// </summary>
        Task<AdminPermissionDto?> GetPermissionByIdAsync(long id);

        /// <summary>
        /// 新增权限（受 module:action 命名规则约束）
        /// </summary>
        Task<AdminPermissionDto> CreatePermissionAsync(SavePermissionRequest request);

        /// <summary>
        /// 修改指定权限（受内置核心权限与角色引用约束）
        /// </summary>
        Task<AdminPermissionDto> UpdatePermissionAsync(long id, SavePermissionRequest request);

        /// <summary>
        /// 软删除指定权限（逻辑删除：修改 Status 为 0）
        /// </summary>
        Task DeletePermissionAsync(long id);

        /// <summary>
        /// 获取层级权限树（供前端与 WPF 角色赋权树控件渲染）
        /// </summary>
        Task<List<PermissionNodeDto>> GetPermissionTreeAsync();
    }
}
