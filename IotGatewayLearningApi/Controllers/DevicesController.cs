using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace IotGatewayLearningApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController:ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        /// <summary>
        /// 分页查询设备
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="keyword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize(Policy = PermissionCodes.DeviceRead)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PageResult<DeviceListItemDto>>>> GetPaged(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken cancellationToken = default)
        {
            // 1. 调用业务层。
            var result = await _deviceService.GetPagedAsync(pageIndex, pageSize, keyword, cancellationToken);

            // 2. 包装统一响应，返回HTTP 200。
            return Ok(new ApiResponse<PageResult<DeviceListItemDto>>
            {
                Code = 200,
                Message = "设备查询成功",
                Data = result
            });
        }

        /// <summary>
        /// 获取设备详情
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize(Policy = PermissionCodes.DeviceRead)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<DeviceDetailDto>>> GetDetail([FromRoute]long id, CancellationToken cancellationToken =default)
        {
            //调用业务层
            var result = await _deviceService.GetDetailAsync(id, cancellationToken);

            //包装统一响应
            return Ok(new ApiResponse<DeviceDetailDto>
            {
                Code = 200,
                Message = "查询设备详情成功",
                Data = result
            });
        }

        /// <summary>
        /// 新增设备
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DeviceDetailDto>>> Create([FromBody] CreateDeviceRequest request, CancellationToken cancellationToken = default)
        {
            //调用业务层
            var result = await _deviceService.CreateAsync(request, cancellationToken);

            //包装统一响应
            return Ok(new ApiResponse<DeviceDetailDto>
            {
                Code = 201,
                Message = "添加新设备成功",
                Data = result
            });
        }

        /// <summary>
        /// 修改设备
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<DeviceDetailDto>>> Update(long id, [FromBody]UpdateDeviceRequest request, 
            CancellationToken cancellationToken = default)
        {
            //调用业务层
            var result = await _deviceService.UpdateAsync(id, request, cancellationToken);

            //包装统一响应
            return Ok(new ApiResponse<DeviceDetailDto>
            {
                Code = 200,
                Message = "修改设备成功",
                Data = result
            });
        }

        /// <summary>
        /// 软删除设备
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize(Policy = PermissionCodes.DeviceConfigure)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object?>>> Disable(long id, CancellationToken cancellationToken = default)
        {
            //调用业务层
            await _deviceService.DisableAsync(id, cancellationToken);

            //包装统一响应
            return Ok(new ApiResponse<object?>
            {
                Code = 200,
                Message = "软删除设备成功",
                Data = null
            });
        }
    }
}
