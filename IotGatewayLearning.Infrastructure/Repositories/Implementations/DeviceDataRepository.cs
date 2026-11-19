using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Data;
using IotGatewayLearning.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories.Implementations
{
    public sealed class DeviceDataRepository : IDeviceDataRepository
    {
        private readonly AppDbContext _db;

        public DeviceDataRepository(AppDbContext db)
        {
            _db = db;
        }
        public async Task AddAsync(DeviceData entity, CancellationToken cancellationToken = default)
        {
            // 只加入 EF Core 变更跟踪器，还没有真正 INSERT。
            // ⚠️ 必须 await：AddAsync 内部可能触发数据库值生成（如自增键回填），
            //    不等待就会在实体还没真正进入 Added 状态时提前返回，
            //    紧接着的 SaveChangesAsync 可能漏掉这条记录。
            await _db.DeviceData.AddAsync(entity, cancellationToken);
        }

        

        public Task<DeviceData?> FindLatestAsync(string deviceCode, string pointCode, CancellationToken cancellationToken = default)
        {
            return _db.DeviceData
                .AsNoTracking()
                .Where(x => x.DeviceCode == deviceCode && x.PointCode == pointCode)
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
