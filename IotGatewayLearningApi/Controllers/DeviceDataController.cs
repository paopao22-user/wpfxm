using IotGatewayLearning.Application.DTOs.DeviceData.Requests;
using IotGatewayLearning.Application.DTOs.DeviceData.Responses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/device-data")]
    public class DeviceDataController: ControllerBase
    {
        private readonly IDeviceDataService _deviceDataService;

        public DeviceDataController(IDeviceDataService deviceDataService)
        {
            _deviceDataService = deviceDataService;
        }

        /// <summary>
        /// 设备数据上报。
        /// </summary>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPost("report")]
        public async Task<ActionResult<ApiResponse<DeviceDataResponse>>> Report(DeviceDataReportRequest request, CancellationToken cancellationToken = default)
        {
            var data = await _deviceDataService.ReportAsync(request, cancellationToken);

            return Ok(new ApiResponse<DeviceDataResponse>
            {
                Code = 200,
                Message = "设备数据接收成功",
                Data = data
            });
        }


        /// <summary>
        /// 查询某台设备某个点位的最新采样值。
        /// </summary>
        [Authorize(Policy = PermissionCodes.DeviceRead)]
        [HttpGet("latest")]
        public async Task<ActionResult<ApiResponse<DeviceDataResponse>>> GetLatest(
            [FromQuery] string deviceCode,
            [FromQuery] string pointCode,
            CancellationToken cancellationToken = default)
        {
            var data = await _deviceDataService.GetLatestAsync(deviceCode,pointCode,cancellationToken);

            if (data is null)
            {
                return NotFound(new ApiResponse<DeviceDataResponse>
                {
                    Code = 404,
                    Message = $"设备 {deviceCode} 的点位 {pointCode} 暂无采样数据",
                    Data = null
                });
            }

            return Ok(new ApiResponse<DeviceDataResponse>
            {
                Code = 200,
                Message = "最新数据查询成功",
                Data = data
            });
        }
    }
}
