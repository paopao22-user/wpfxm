using IotGatewayLearning.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// 设备采样历史仓储契约。
    /// </summary>
    public interface IDeviceDataRepository
    {
        /// <summary>
        /// 将一条采样记录加入 EF Core 变更跟踪器。
        /// 此时还没有真正写入数据库，需要 UnitOfWork 提交。
        /// </summary>
        Task AddAsync(
            DeviceData entity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 按设备编号和点位编号查询最新一条采样记录。
        /// 排序规则：occurred_at 倒序，时间相同时再按 id 倒序。
        /// </summary>
        Task<DeviceData?> FindLatestAsync(
            string deviceCode,
            string pointCode,
            CancellationToken cancellationToken = default);

        

    }
}
