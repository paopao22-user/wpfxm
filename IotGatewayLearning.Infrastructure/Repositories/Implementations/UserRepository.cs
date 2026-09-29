using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Data;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context):base(context)
        {
            
        }

        /// <summary>
        /// 通过用户id来查找用户
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<User?> GetByIdWithRbacAsync(long userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AsNoTracking()
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        }

        /// <summary>
        /// 通过用户名查找用户
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public async Task<User?> GetByUsernameWithRbacAsync(string username)
        {
            return await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.Username == username);
        }

        /// <summary>
        /// 物理数据库分页查询用户列表
        /// </summary>
        public async Task<(List<User> Users, long Total)> GetPagedUsersAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken = default)
        {
            // 步骤 1：创建不跟踪实体的 IQueryable 查询树，降低内存消耗
            var query = _context.Users.AsNoTracking();

            // 步骤 2：如果传入了查询关键字，动态拼装 WHERE 过滤条件
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();

                query = query.Where(x => x.Username.Contains(key) || x.RealName.Contains(key));
            }

            // 步骤 3：第一条 SQL 语句 —— 查询符合条件的总记录条数
            var total = await query.CountAsync(cancellationToken);

            // 步骤 4：第二条 SQL 语句 —— 按主键升序排序，并通过 Skip/Take 实现物理切片
            var users = await query.OrderBy(x => x.Id)
                                    .Skip((page - 1) * pageSize)    // 物理偏移跳过行数
                                    .Take(pageSize)                 // 仅读取目标条数
                                    .ToListAsync(cancellationToken);

            // 步骤 5：将数据集合与总数打包返回
            return (users, total);
        }
    }
}
