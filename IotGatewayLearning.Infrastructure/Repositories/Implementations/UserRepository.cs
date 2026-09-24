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
    }
}
