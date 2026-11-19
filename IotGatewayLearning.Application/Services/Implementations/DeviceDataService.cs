using IotGatewayLearning.Application.DTOs.DeviceData.Requests;
using IotGatewayLearning.Application.DTOs.DeviceData.Responses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
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
    public sealed class DeviceDataService : IDeviceDataService
    {
        //依赖
        private readonly IDeviceDataRepository _deviceDataRepository;
        private readonly IDeviceRepository _deviceRepository;

        private readonly IDevicePointRepository _devicePointRepository;

        private readonly IUnitOfWork _unitOfWork;

        // 构造函数注入依赖
        public DeviceDataService(IDeviceDataRepository deviceDataRepository, 
                                IDeviceRepository deviceRepository, 
                                IDevicePointRepository devicePointRepository, 
                                IUnitOfWork unitOfWork)
        {
            _deviceDataRepository = deviceDataRepository;
            _deviceRepository = deviceRepository;
            _devicePointRepository = devicePointRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DeviceDataResponse?> GetLatestAsync(string deviceCode, string pointCode, CancellationToken cancellationToken = default)
        {
            // 1. 参数为空直接拒绝，不查数据库。
            if (string.IsNullOrWhiteSpace(deviceCode)
                || string.IsNullOrWhiteSpace(pointCode))
            {
                throw new AppException("设备编号和点位编号不能为空", 400);
            }

            // 2. 去掉首尾空格。
            var code = deviceCode.Trim();
            var point = pointCode.Trim();

            // 3. 查询最近一条。
            var entity = await _deviceDataRepository.FindLatestAsync(code, point, cancellationToken);

            // 4. 没有数据返回 null，由 Controller 转 404。
            if (entity is null)
            {
                return null;
            }

            // 5. Entity -> Response。
            return ToDto(entity);
        }

        public async Task<DeviceDataResponse> ReportAsync(DeviceDataReportRequest request, CancellationToken cancellationToken = default)
        {
            // 1. 规范化编号
            var deviceCode = request.DeviceCode?.Trim();
            var pointCode = request.PointCode?.Trim();

            if (string.IsNullOrWhiteSpace(deviceCode) || string.IsNullOrWhiteSpace(pointCode))
            {
                throw new AppException("设备编号或点位编号不能为空", 400);
            }

            // 2. 规范化质量码和来源
            var quality = NormalizeQuality(request.Quality);
            var source = NormalizeSource(request.Source);

            // 3. 处理设备时间
            var occurredAtUtc = NormalizeOccurredAt(request.OccurredAt);

            // 4. 查询设备
            var device = await _deviceRepository.FindByCodeAsync(deviceCode, cancellationToken);

            if (device is null)
            {
                throw new AppException("设备不存在或已删除", 404);
            }

            if (device.Status != 1)
            {
                throw new AppException("设备已停用，不能上报数据", 404);
            }

            // 5. 查询点位
            var point = await _devicePointRepository.FindEnabledAsync(device.Id, pointCode, cancellationToken);

            if(point is null)
            {
                throw new AppException("点位不存在或已停用", 404);
            }

            // 6. 服务端时间
            var now = DateTime.UtcNow;

            // 7. 创建实体
            var entity = new DeviceData
            {
                DeviceCode = device.DeviceCode,
                PointCode = point.PointCode,
                ValueDecimal = request.Value,
                RawValue = NormalizeOptional(request.RawValue),
                Unit = NormalizeOptional(request.Unit) ?? NormalizeOptional(point.Unit),
                Quality = quality,
                Source = source,
                OccurredAt = occurredAtUtc,
                ReceivedAt = now,
                CreatedAt = now
            };

            // 8. 登记 + 提交
            await _deviceDataRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 9. Entity -> Response
            return ToDto(entity);
        }

        /// <summary>
        /// 规范化与校验工具方法
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string NormalizeQuality(string value)
        {
            var quality = value?.Trim();

            if(string.Equals(quality, "Good", StringComparison.OrdinalIgnoreCase))
            {
                return "Good";
            }
            else if (string.Equals(quality, "Bad", StringComparison.OrdinalIgnoreCase))
            {
                return "Bad";
            }
            else if (string.Equals(quality, "Uncertain", StringComparison.OrdinalIgnoreCase))
            {
                return "Uncertain";
            }
            else
            {
                throw new AppException("quality 只能是 Good、Bad 或 Uncertain", 400);
            }
        }

        private static readonly HashSet<string> AllowedSources = new(StringComparer.OrdinalIgnoreCase)
        {
            "serial",
            "modbus",
            "mqtt",
            "http",
            "simulator",
            "manual"
        };

        private static string NormalizeSource(string value)
        {
            var source = value?.Trim().ToLowerInvariant();  // 统一转为小写，方便后续比较

            if (string.IsNullOrWhiteSpace(source) || !AllowedSources.Contains(source))
            {
                throw new AppException("source 不在允许的范围内", 400);
            }
            
            return source;
        }


        private static DateTime NormalizeOccurredAt(DateTime value)
        {
            if(value == default)
            {
                throw new AppException("occurredAt 不能为空", 400);
            }

            if (value.Kind == DateTimeKind.Unspecified)
            {
                throw new AppException(
                    "occurredAt 必须带时区，例如 2026-09-12T10:30:00Z 或 +08:00",
                    400);
            }

            return value.ToUniversalTime();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Entity -> Response
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static DeviceDataResponse ToDto(DeviceData entity)
        {
            return new DeviceDataResponse
            {
                Id = entity.Id,
                DeviceCode = entity.DeviceCode,
                PointCode = entity.PointCode,
                Value = entity.ValueDecimal,
                RawValue = entity.RawValue,
                Unit = entity.Unit,
                Quality = entity.Quality,
                Source = entity.Source,
                // ⚠️ MySQL 的 datetime 字段不带时区，EF Core 从数据库读出来的
                //    DateTime.Kind 会是 Unspecified。如果直接返回，JSON 里就没有 Z，
                //    前端会误以为是本地时间（东八区会差 8 小时）。
                //    数据库里存的本来就是 UTC（ReportAsync 里已经 ToUniversalTime），
                //    所以这里显式标记成 UTC，让 System.Text.Json 序列化出 "...Z"。
                OccurredAt = DateTime.SpecifyKind(entity.OccurredAt, DateTimeKind.Utc),
                ReceivedAt = DateTime.SpecifyKind(entity.ReceivedAt, DateTimeKind.Utc)
            };
        }
    }
}
