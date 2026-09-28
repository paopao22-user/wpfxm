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
        /// <summary>
        /// 根据目标用户id来查找用户
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <returns></returns>
        Task<AdminUserDto?> GetUserByIdAsync(long targetUserId);

        /// <summary>
        /// 根据keyword查找到所有包含keyword的List集合
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        Task<List<AdminUserDto>> GetUsersAsync(string? keyword);

        /// <summary>
        /// 新增管理员用户
        /// </summary>
        /// <param name="request">新增用户的入参模型</param>
        /// <returns>创建成功后脱敏的用户 DTO（包含生成的自增 ID）</returns>
        Task<AdminUserDto> CreateUserAsync(CreateUserRequest request);


        /// <summary>
        /// 修改用户基础资料与状态
        /// </summary>
        /// <param name="id">要修改的目标用户 ID</param>
        /// <param name="request">修改的数据内容</param>
        /// <param name="actorId">当前执行操作的操作者用户 ID</param>
        /// <returns>修改成功后的用户脱敏 DTO</returns>
        Task<AdminUserDto> UpdateUserAsync(long id,UpdateUserRequest request, long actorId);

        /// <summary>
        /// 删除指定用户（含安全防护规则）
        /// </summary>
        /// <param name="id">要删除的目标用户 ID</param>
        /// <param name="actorId">当前执行操作的操作者用户 ID</param>
        Task DeleteUserAsync(long id, long actorId);

        /// <summary>
        /// 获取角色列表：支持根据关键字过滤编码或名称
        /// </summary>
        /// <param name="keyword">搜索关键字</param>
        /// <returns>脱敏后的角色 DTO 列表</returns>
        Task<List<AdminRoleDto>> GetRolesAsync(string? keyword);

        /// <summary>
        /// 根据主键 ID 精确查询单个角色详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<AdminRoleDto> GetRoleByIdAsync(long id);

        /// <summary>
        /// 新增角色（含编码规范化与唯一性校验）
        /// </summary>
        /// <param name="request">保存角色请求模型</param>
        /// <returns>新增成功后的角色 DTO（包含生成的自增 ID）</returns>
        Task<AdminRoleDto> CreateRoleAsync(SaveRoleRequest request);

        /// <summary>
        /// 修改角色
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<AdminRoleDto> UpdateRoleAsync(long id, SaveRoleRequest request);

        /// <summary>
        /// 删除指定角色（逻辑软删除，将 Status 置为 0）
        /// </summary>
        /// <param name="id">要删除的目标角色 ID</param>
        /// <returns></returns>
        Task DeleteRoleAsync(long id);

        /// <summary>
        /// 获取权限列表：支持根据关键字对权限编码（Code）或显示名称（Name）进行模糊匹配
        /// </summary>
        /// <param name="keyword">搜索关键字（可为空）</param>
        /// <returns>脱敏后的权限 DTO 列表集合</returns>
        Task<List<AdminPermissionDto>> GetPermissionsAsync(string? keyword);

        /// <summary>
        /// 根据权限主键 ID 精确查询单个权限详情
        /// </summary>
        /// <param name="id">权限主键 ID</param>
        /// <returns>查询到则返回脱敏后的 AdminPermissionDto，未查到返回 null</returns>
        Task<AdminPermissionDto?> GetPermissionByIdAsync(long id);

        /// <summary>
        /// 新增权限（含编码 module:action 规范化与全局唯一性防重校验）
        /// </summary>
        /// <param name="request">保存权限请求模型</param>
        /// <returns>新增成功后的权限 DTO（包含数据库自动生成的自增 ID）</returns>
        Task<AdminPermissionDto> CreatePermissionAsync(SavePermissionRequest request);

        /// <summary>
        /// 修改权限定义（含内置核心权限保护、已分配角色保护与排他防重）
        /// </summary>
        /// <param name="id">要修改的目标权限 ID</param>
        /// <param name="request">保存权限请求模型</param>
        /// <returns>修改成功后的权限 DTO</returns>
        Task<AdminPermissionDto> UpdatePermissionAsync(long id,SavePermissionRequest request);

        /// <summary>
        /// 软删除指定权限（将 Status 置为 0，含内置核心权限保护与角色关联检查）
        /// </summary>
        /// <param name="id">权限主键 ID</param>
        Task DeletePermissionAsync(long id);


        /// <summary>
        /// 获取层级权限树（供前端角色授权弹窗渲染为分组复选树）
        /// </summary>
        /// <returns>按业务模块分组聚类后的权限树节点列表</returns>
        Task<List<PermissionNodeDto>> GetPermissionTreeAsync();
    }
}
