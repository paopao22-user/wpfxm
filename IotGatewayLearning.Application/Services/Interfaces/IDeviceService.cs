using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using IotGatewayLearning.Common.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 业务服务接口是直接给 Controller 调用的业务用例契约，**面向客户端输入输出（DTO）**
    /// </summary>
    public interface IDeviceService
    {
        // 1. 设备台账分页列表展示
        Task<PageResult<DeviceListItemDto>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default);

        // 2. 查看单台设备全量详情（包含测点列表）
        Task<DeviceDetailDto> GetDetailAsync(long id, CancellationToken cancellationToken = default);

        // 3. 新增设备台账
        Task<DeviceDetailDto> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default);

        // 4. 修改设备台账基础资料
        Task<DeviceDetailDto> UpdateAsync(long id, UpdateDeviceRequest request, CancellationToken cancellationToken = default);

        // 5. 停用设备（软删除）
        Task DisableAsync(long id, CancellationToken cancellationToken = default);
    }
}
