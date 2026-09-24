using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }    //UnitOfWork 对外提供一个“用户仓储”

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
