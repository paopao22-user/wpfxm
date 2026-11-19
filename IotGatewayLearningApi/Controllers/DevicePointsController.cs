using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api")]
    public sealed class DevicePointsController : ControllerBase
    {
        private readonly IDevicePointService _devicePoint;

        public DevicePointsController(IDevicePointService devicePoint)
        {
            _devicePoint = devicePoint;
        }

        /// <summary>
        /// 9.2 查询指定设备名下的点位列表（支持按编码或名称模糊过滤）
        /// </summary>
        /// <param name="deviceId">所属设备主键 ID</param>
        /// <param name="keyword">可选过滤关键词（点位编码或名称）</param>
        /// <returns>点位列表响应载荷</returns>
        [HttpGet("devices/{deviceId:long}/points")]
        [Authorize(Policy = PermissionCodes.DeviceRead)]
        public async Task<IActionResult> List(long deviceId, [FromQuery] string? Keyword)
        {
            var data = await _devicePoint.ListAsync(deviceId, Keyword);

            return Ok(new ApiResponse<List<DevicePointDto>>
            {
                Code = 200,
                Message = "点位查询成功",
                Data = data
            });
        }


        /// <summary>
        /// 9.3 查询指定点位详情
        /// </summary>
        /// <param name="id">点位主键 ID</param>
        /// <returns>点位详情响应载荷</returns>
        [HttpGet("device-points/{id:long}")]
        [Authorize(Policy = PermissionCodes.DeviceRead)]
        public async Task<ActionResult<ApiResponse<DevicePointDto>>> Get(long id)
        {
            // 1. 调用业务层执行双重校验与数据拉取
            var data = await _devicePoint.GetAsync(id);

            // 2. 包装为统一规范的 ApiResponse 响应外壳返回 HTTP 200
            return Ok(new ApiResponse<DevicePointDto>
            {
                Code = 200,
                Message = "点位详情查询成功",
                Data = data
            });
        }


        /// <summary>
        /// 9.4 为指定设备新增采集点位
        /// </summary>
        [HttpPost("devices/{deviceId:long}/points")]
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        public async Task<ActionResult<ApiResponse<DevicePointDto>>> Create(long deviceId, CreateDevicePointRequest request)
        {
            // 1. 调用业务服务层执行校验、查重与物理落盘
            var data = await _devicePoint.CreateAsync(deviceId, request);

            // 2. 包装为统一规范的 ApiResponse 响应外壳返回 HTTP 200
            return Ok(new ApiResponse<DevicePointDto>
            {
                Code = 200,
                Message = "新增点位成功",
                Data = data
            });
        }

        /// <summary>
        /// 9.5 为知道设备修改采集点位
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("device-points/{id:long}")]
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        public async Task<ActionResult<ApiResponse<DevicePointDto>>> Update(long id, UpdateDevicePointRequest request)
        {
            // 1. 调用业务服务层执行校验、查重与物理落盘
            var data = await _devicePoint.UpdateAsync(id, request);
            // 2. 包装为统一规范的 ApiResponse 响应外壳返回 HTTP 200
            return Ok(new ApiResponse<DevicePointDto>
            {
                Code = 200,
                Message = "更新点位成功",
                Data = data
            });
        }


        /// <summary>
        /// 9.6 停用点位（软删除）
        /// </summary>
        /// <param name="id">点位主键 ID</param>
        /// <returns>操作成功标识</returns>
        [HttpDelete("device-points/{id:long}")]
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
        {
            await _devicePoint.DisableAsync(id);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "停用点位成功",
                Data = true
            });
        }

        /// <summary>
        /// 9.7 恢复/启用点位
        /// </summary>
        /// <param name="id">点位主键 ID</param>
        /// <returns>操作成功标识</returns>
        [HttpPut("device-points/{id:long}/enable")]
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        public async Task<ActionResult<ApiResponse<bool>>> Enable(long id)
        {
            await _devicePoint.EnableAsync(id);

            return Ok(new ApiResponse<bool>
            {
                Code = 200,
                Message = "启用点位成功",
                Data = true
            });
        }
    }
}
