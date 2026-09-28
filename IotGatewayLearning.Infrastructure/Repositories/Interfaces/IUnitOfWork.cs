using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    /// <summary>
    /// 工作单元：统一管理系统内所有实体的仓储实例与跨表事务提交
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// 用户专属仓储
        /// </summary>
        IUserRepository Users { get; }    //UnitOfWork 对外提供一个“用户仓储”

        /// <summary>
        /// 角色仓储（使用通用泛型仓储）
        /// </summary>
        IRepository<Role> Roles { get; }

        /// <summary>
        /// 用户-角色关联仓储（用于关联校验与授权分配）
        /// </summary>
        IRepository<UserRole> UserRoles { get; }

        /// <summary>
        /// 权限目录仓储（使用通用泛型仓储）
        /// </summary>
        IRepository<Permission> Permissions { get; }

        /// <summary>
        /// 角色-权限关联仓储（用于关联校验与授权分配）
        /// </summary>
        IRepository<RolePermission> RolePermissions { get; }

        /// <summary>
        /// 统一异步提交事务，将所有追踪实体的变更落盘写入数据库
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
