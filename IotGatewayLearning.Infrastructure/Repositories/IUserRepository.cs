using IotGatewayLearning.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    public interface IUserRepository:IRepository<User>
    {
        Task<User?> GetByUsernameWithRbacAsync(string username);
    }
}
