using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 设备采集点位业务服务用例契约
    /// </summary>
    public interface IDevicePointService
    {
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="Keyword"></param>
        /// <returns></returns>
        Task<List<DevicePointDto>> ListAsync(long deviceId, string? Keyword);

        /// <summary>
        /// 详情查询
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<DevicePointDto> GetAsync(long id);

        // 9.4 添加点位
        Task<DevicePointDto> CreateAsync(long deviceId, CreateDevicePointRequest request);

        // 9.5 修改点位
        Task<DevicePointDto> UpdateAsync(long id, UpdateDevicePointRequest request);

        // 9.6 停用点位
        Task DisableAsync(long id);

        // 9.7 恢复点位
        Task EnableAsync(long id);
    }
}
