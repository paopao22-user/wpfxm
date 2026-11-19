using IotGatewayLearning.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// 设备采集点位专有仓储契约
    /// </summary>
    public interface IDevicePointRepository
    {
        // 1. 检查所属设备是否存在且未被软删除
        Task<bool> DeviceExistAsync(long deviceId);

        // 2. 根据设备 ID 查询其名下的点位列表（支持按编码或名称模糊过滤）
        Task<IReadOnlyList<DevicePoint>> ListAsync(long deviceId, string? Keyword);

        // 3. 根据点位主键 ID 查询单个点位跟踪实体（供编辑/修改使用）
        Task<DevicePoint?> FindAsync(long id);


        // 4. 同设备内点位编码防重校验：排除指定排除项（excludingId，用于修改时排查自身）
        Task<bool> ExistsCodeAsync(long deviceId, string code, long? excludingId);

        // 5. 附加新点位实体到 EF Core 状态追踪树上（Added 状态）
        void Add(DevicePoint entity);   


        Task<DevicePoint?> FindEnabledAsync(long deviceId, string pointCode, CancellationToken cancellationToken = default);
    }
}
