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
    public class DeviceRepository : IDeviceRepository
    {
        private readonly AppDbContext _db;

        public DeviceRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 新增设备
        /// </summary>
        /// <param name="device"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
        {
            await _db.Devices.AddAsync(device, cancellationToken); 
        }

        /// <summary>
        /// 查重方法必须包括软删除记录
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<bool> ExistsByCodeAsync(string deviceCode, CancellationToken cancellationToken = default)
        {
            return _db.Devices.IgnoreQueryFilters().AsNoTracking().AnyAsync(x => x.DeviceCode == deviceCode, cancellationToken);
        }

        /// <summary>
        /// 根据设备编号查找设备
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Device?> FindByCodeAsync(string deviceCode, CancellationToken cancellationToken = default)
        {
            return _db.Devices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DeviceCode == deviceCode, cancellationToken);
        }

        /// <summary>
        /// 修改查重的完整方法
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="excludedId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<bool> ExistsByCodeAsync(string deviceCode, long excludedId, CancellationToken cancellationToken = default)
        {
            return _db.Devices.IgnoreQueryFilters().AsNoTracking().AnyAsync(x => x.DeviceCode == deviceCode && x.Id != excludedId ,cancellationToken);
        }

        /// <summary>
        /// 根据设备id查询设备,需要追踪,修改设备需要追踪
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<Device?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return _db.Devices.AsTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == 0, cancellationToken);
        }

        /// <summary>
        /// 根据设备id查询一台未删除设备，并加载点位.  查询设备详情不需要追踪
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<Device?> FindByIdWithPointsAsync(long id, CancellationToken cancellationToken = default)
        {
            return _db.Devices.AsNoTracking().Include(x => x.Points).FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == 0,cancellationToken);
        }


        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="keyword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(IReadOnlyList<Device> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default)
        {
            // 1. 建立设备查询，列表只读，不需要实体跟踪。
            var query = _db.Devices.AsNoTracking().Where(x => x.IsDeleted == 0);

            // 2. 有有效关键词时，追加搜索条件。
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var value = keyword.Trim();

                query = query.Where(x => x.DeviceCode.Contains(value) 
                || x.Name.Contains(value) || (x.Location != null && x.Location.Contains(value)));
            }

            // 3. 统计所有符合条件的设备数量。    筛选后统计，统计后分页。
            var totalCount = await query.CountAsync(cancellationToken);

            // 4. 查询当前页。
            var items = await query.OrderBy(x => x.Id)
                                .Skip((pageIndex - 1) * pageSize)
                                .Take(pageSize).ToListAsync(cancellationToken);


            // 5. 同时返回当前页和总数量。
            return (items, totalCount);
        }
    }
}
