using IotGatewayLearning.Application.DTOs.DeviceData.Requests;
using IotGatewayLearning.Application.DTOs.DeviceData.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Interfaces
{
    /// <summary>
    /// 设备采样数据业务接口。
    /// </summary>
    public interface IDeviceDataService
    {
        /// <summary>
        /// 接收一条设备采样数据并保存。
        /// </summary>
        Task<DeviceDataResponse> ReportAsync(
            DeviceDataReportRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询某台设备某个点位的最新一条采样数据。
        /// 没有数据时返回 null，由 Controller 转换为 404。
        /// </summary>
        Task<DeviceDataResponse?> GetLatestAsync(
            string deviceCode,
            string pointCode,
            CancellationToken cancellationToken = default);
    }
}
