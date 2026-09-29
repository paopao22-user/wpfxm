using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Common.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 用户领域业务服务契约接口
    /// 涵盖用户的全量增删改查、物理分页以及用户-角色的分配关系维护
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 根据用户主键 ID 异步查询用户详情
        /// </summary>
        Task<AdminUserDto?> GetUserByIdAsync(long targetUserId);

        /// <summary>
        /// 根据关键字模糊查询用户平铺列表（全量）
        /// </summary>
        Task<List<AdminUserDto>> GetUsersAsync(string? keyword);

        /// <summary>
        /// 物理数据库分页查询用户列表
        /// </summary>
        Task<PageResult<AdminUserDto>> GetUserPageListAsync(
            int page,
            int pageSize,
            string? keyword,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 新增管理员/操作员用户
        /// </summary>
        Task<AdminUserDto> CreateUserAsync(CreateUserRequest request);

        /// <summary>
        /// 修改用户基本信息与状态
        /// </summary>
        Task<AdminUserDto> UpdateUserAsync(long id, UpdateUserRequest request, long actorId);

        /// <summary>
        /// 软删除/禁用指定用户
        /// </summary>
        Task DeleteUserAsync(long id, long actorId);

        /// <summary>
        /// 查询指定用户当前已拥有的角色 ID 集合
        /// </summary>
        Task<List<long>> GetUserRoleIdsAsync(long userId);

        /// <summary>
        /// 为指定用户完整分配/替换角色列表（基于差集算法幂等更新）
        /// </summary>
        Task SetUserRolesAsync(long userId, AssignIdsRequest request, long actorId);
    }
}
