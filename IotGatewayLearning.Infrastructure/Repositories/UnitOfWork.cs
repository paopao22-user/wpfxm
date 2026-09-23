using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    // 实现 IUnitOfWork 接口，对外提供统一的数据工作单元
    public class UnitOfWork : IUnitOfWork
    {
        // 核心私有字段：持有的数据库上下文实例
        private readonly AppDbContext _context;

        // 私有缓存字段：用户仓储接口实例（初态为 null）
        private IUserRepository? _users;

        // 构造函数：通过依赖注入获取上下文。这样仓储层和工作单元层共享同一个数据库上下文实例，保证事务的一致性。
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // 延迟初始化：暴露用户仓储,这个是一个懒加载的属性，只有在第一次访问时才会创建 Repository<User> 实例
        public IUserRepository Users => _users ??= new UserRepository(_context);

        

        // 统一异步提交持久化方法
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
