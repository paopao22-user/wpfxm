using IotGatewayLearning.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository:IRepository<User>
    {
        //通过用户名来查找用户
        Task<User?> GetByUsernameWithRbacAsync(string username);

        //通过用户id来查找用户   CancellationToken：：若客户端取消请求，数据库查询可以一起取消。
        Task<User?> GetByIdWithRbacAsync(long userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 物理数据库分页查询用户列表
        /// </summary>
        /// <param name="page">页码（从 1 开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">用户名或真实姓名关键字</param>
        /// <param name="cancellationToken">异步取消令牌</param>
        /// <returns>返回包含用户实体列表与总记录数的元组</returns>
        Task<(List<User> Users, long Total)> GetPagedUsersAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken = default);
    }
}
