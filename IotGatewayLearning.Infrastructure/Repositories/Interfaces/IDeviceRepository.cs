using IotGatewayLearning.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories.Interfaces
{
    public interface IDeviceRepository
    {
        // 1. 分页查询未删除的设备，返回设备列表与总行数
        Task<(IReadOnlyList<Device> Items,int TotalCount)> GetPagedAsync
            (int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default);

        // 2. 根据 ID 查询单台设备，并显式 Include 预加载测点集合 Points
        Task<Device?> FindByIdWithPointsAsync(long id, CancellationToken cancellationToken = default);


        // 3. 新增设备时的防重校验：检查全局是否有重复的 device_code
        Task<bool> ExistsByCodeAsync(string deviceCode, CancellationToken cancellationToken = default);


        // 4. 将新创建的 Device 实体附加到 EF Core 状态跟踪树上
        Task AddAsync(Device device, CancellationToken cancellationToken = default);

        // 5. 根据 ID 查询单台设备（普通查询，不加载子表，用于更新或删除前的跟踪）
        Task<Device?> FindByIdAsync(long id, CancellationToken cancellationToken = default);

        // 6. 修改设备时的排重校验：检查除了当前这台设备（excludeId）外，还有没有别人占用该编号
        Task<bool> ExistsByCodeAsync(string deviceCode, long excludedId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据设备编号查询设备实体。
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Device?> FindByCodeAsync(string deviceCode, CancellationToken cancellationToken = default);
    }
}
