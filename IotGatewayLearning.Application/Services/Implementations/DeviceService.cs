using AutoMapper;
using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Common.Responses;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using IotGatewayLearning.Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Implementations
{
    public class DeviceService:IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DeviceService(IDeviceRepository deviceRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _deviceRepository = deviceRepository;

            _unitOfWork = unitOfWork;

            _mapper = mapper;
        }

        /// <summary>
        /// 新增设备
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<DeviceDetailDto> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default)
        {
            // 1. 规范化设备编号：去掉首尾空白。
            request.DeviceCode = request.DeviceCode.Trim();

            // 2. 查重，包含已经软删除的设备
            if(await _deviceRepository.ExistsByCodeAsync(request.DeviceCode, cancellationToken))
            {
                throw new AppException("设备编号已存在", 400);
            }

            // 3. 请求DTO转换成新的设备实体。
            var device = _mapper.Map<Device>(request);

            // 4. 设置服务端维护的初始状态。
            device.Status = 1;
            device.IsDeleted = 0;
            device.ConnectionStatus = 0;
            device.RunStatus = 0;

            // 5. 本教学版本由Service设置创建、更新时间。
            var now = DateTime.UtcNow;
            device.CreatedAt = now;
            device.UpdatedAt = now;

            // 6. 登记新增，此时不是保存完成。
            await _deviceRepository.AddAsync(device, cancellationToken);

            // 7. 真正写入数据库，并取得数据库生成的主键。
            await _unitOfWork.SaveChangesAsync();

            // 8. 返回创建后的详情DTO。
            return _mapper.Map<DeviceDetailDto>(device);
        }


        /// <summary>
        /// 软删除
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task DisableAsync(long id, CancellationToken cancellationToken = default)
        {
            // 1. 查询未删除、被EF跟踪的设备。
            var device = await _deviceRepository.FindByIdAsync(id, cancellationToken);

            // 2. 不存在或已经软删除，不返回假成功。
            if(device == null)
            {
                throw new AppException("设备不存在", 404);
            }

            // 3. 从正常台账中移除，同时停用。
            device.IsDeleted = 1;
            device.Status = 0;

            // 4. 更新时间由服务端维护。
            device.UpdatedAt = DateTime.UtcNow;

            // 5. 真正执行数据库UPDATE。
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 判断存在，再把设备和点位映射成 DTO
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<DeviceDetailDto> GetDetailAsync(long id, CancellationToken cancellationToken = default)
        {
            // 1. 查询设备及其点位。
            var device = await _deviceRepository.FindByIdWithPointsAsync(id, cancellationToken);

            // 2. 不存在或已软删除，转为业务异常。
            if (device == null)
            {
                throw new AppException("设备不存在", 404);
            }

            // 3. Entity -> DTO，包括关联点位集合。
            return _mapper.Map<DeviceDetailDto>(device);
        }

        /// <summary>
        /// 把数据库结果变成接口需要的分页数据
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="keyword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<PageResult<DeviceListItemDto>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default)
        {
            // 1. 页码至少是1。
            pageIndex = pageIndex < 1 ? 1 : pageIndex;

            // 2. 每页数量必须在1～100之间，否则使用20。
            pageSize = pageSize is < 1 or  > 100 ? 20 : pageSize;

            // 3. 调用仓储查询实体列表和总数量。
            var result = await _deviceRepository.GetPagedAsync(pageIndex, pageSize, keyword, cancellationToken);

            // 4. 将实体集合转换为列表DTO集合。  _mapper.Map < 目标类型 > ( 原材料数据 )
            var items = _mapper.Map<List<DeviceListItemDto>>(result.Items);

            // 5. 使用你当前项目的分页属性。
            return new PageResult<DeviceListItemDto>
            {
                Items = items,
                Total = result.TotalCount,
                Page = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 修改设备
        /// </summary>
        /// <param name="id">目标要修改的设备id</param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<DeviceDetailDto> UpdateAsync(long id, UpdateDeviceRequest request, CancellationToken cancellationToken = default)
        {
            // 1. 规范化准备使用的设备编号。
            request.DeviceCode = request.DeviceCode.Trim();

            // 2. 查询原设备，返回被EF跟踪的实体。
            var device = await _deviceRepository.FindByIdAsync(id, cancellationToken);

            // 3. 不存在或已软删除，不允许修改。
            if(device == null)
            {
                throw new AppException("设备不存在", 404);
            }

            // 4. 查重排除自身，同时包含其他软删除记录。
            if(await _deviceRepository.ExistsByCodeAsync(request.DeviceCode, id, cancellationToken))
            {
                throw new AppException("设备编号已被其他设备使用", 400);
            }

            // 5. 将允许修改的新值写入原对象。
            _mapper.Map(request, device);   //请求中的新值写到同一个device实例中，EF仍然跟踪这个实例

            // 6. 修改时间由服务端设置，创建时间保持不变。
            device.UpdatedAt = DateTime.UtcNow;

            // 7. 检测跟踪实体的变化，并执行UPDATE。
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. 复用详情查询，返回设备和完整点位配置。
            return await GetDetailAsync(id, cancellationToken);
        }
    }
}
