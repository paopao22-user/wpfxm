using IotGatewayLearning.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 角色领域业务服务契约接口
    /// 涵盖角色的全量增删改查、安全防线拦截以及角色-权限的分配管理
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// 获取角色列表（支持根据关键字对 Code 和 Name 进行模糊匹配）
        /// </summary>
        Task<List<AdminRoleDto>> GetRolesAsync(string? keyword);

        /// <summary>
        /// 根据角色主键 ID 查询角色详情
        /// </summary>
        Task<AdminRoleDto?> GetRoleByIdAsync(long id);

        /// <summary>
        /// 新增角色
        /// </summary>
        Task<AdminRoleDto> CreateRoleAsync(SaveRoleRequest request);

        /// <summary>
        /// 修改角色信息（受内置 admin 与用户引用约束）
        /// </summary>
        Task<AdminRoleDto> UpdateRoleAsync(long id, SaveRoleRequest request);

        /// <summary>
        /// 软删除/停用角色（受 admin 保护与引用完整性约束）
        /// </summary>
        Task DeleteRoleAsync(long id);

        /// <summary>
        /// 查询指定角色当前已分配的权限 ID 集合
        /// </summary>
        Task<List<long>> GetRolePermissionIdsAsync(long roleId);

        /// <summary>
        /// 为指定角色完整分配/替换权限集合（基于差集算法实现幂等更新）
        /// </summary>
        Task SetRolePermissionsAsync(long roleId, AssignIdsRequest request);
    }
}
