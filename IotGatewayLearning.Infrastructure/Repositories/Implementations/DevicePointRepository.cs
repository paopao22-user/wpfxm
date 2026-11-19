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
    public sealed class DevicePointRepository : IDevicePointRepository
    {
        private readonly AppDbContext _db;

        // 构造函数：通过依赖注入获取统一的数据库上下文实例
        public DevicePointRepository(AppDbContext db)
        {
            _db = db;
        }

        // 检查所属设备存在且未被软删除
        public Task<bool> DeviceExistAsync(long deviceId)
        {
            return _db.Devices.AnyAsync(x => x.Id == deviceId && x.IsDeleted == 0);
        }

        // 列表查询：AsNoTracking 非追踪，支持关键字模糊搜索
        public async Task<IReadOnlyList<DevicePoint>> ListAsync(long deviceId, string? Keyword)
        {
            // 步骤 1：构造基础查询树，锁定设备范围，同时关闭变更追踪
            var query = _db.DevicePoints.AsNoTracking().Where(x => x.DeviceId == deviceId);   //根据设备id查询到设备点位
            // 步骤 2：检查是否传入了搜索关键字（过滤空字符串和纯空格
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                // 步骤 3：清洗入参，去除首尾多余空格
                var value = Keyword.Trim();

                // 步骤 4：动态追加 WHERE 条件（编码模糊匹配 或 中文名模糊匹配）
                query = query.Where(x => x.PointCode.Contains(value) || x.Name.Contains(value));
            }
            // 步骤 5：按自增 ID 升序排序，并真正触发数据库网络 I/O 查询
            return await query.OrderBy(x => x.Id).ToListAsync();
        }

        // 查单条：跟踪查询，供 Update 使用
        public Task<DevicePoint?> FindAsync(long id)
        {
            return _db.DevicePoints.FirstOrDefaultAsync(x => x.Id == id);
        }

        // 编码防重：检查当前设备下是否已被占用
        public Task<bool> ExistsCodeAsync(long deviceId, string code, long? excludingId)
        {
            return _db.DevicePoints.AsNoTracking().AnyAsync
                (x => x.DeviceId == deviceId && x.PointCode == code && (!excludingId.HasValue || x.Id != excludingId.Value));
        }


        //添加实体
        public void Add(DevicePoint entity)
        {
             _db.DevicePoints.Add(entity);
        }

        public Task<DevicePoint?> FindEnabledAsync(long deviceId, string pointCode, CancellationToken cancellationToken = default)
        {
            return _db.DevicePoints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DeviceId == deviceId
                    && x.PointCode == pointCode
                    && x.Enabled == 1);
        }
    }
}
