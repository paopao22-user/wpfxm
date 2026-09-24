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
    }
}
