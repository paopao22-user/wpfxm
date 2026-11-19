using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using IotGatewayLearning.Application.Services.Interfaces;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using IotGatewayLearning.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Services.Implementations
{
    public class DevicePointService:IDevicePointService
    {
        private readonly IDevicePointRepository _repo;
        private readonly IUnitOfWork _unit;
            
        //构造函数注入
        public DevicePointService(IDevicePointRepository repo, IUnitOfWork unit)
        {
            _repo = repo;
            _unit = unit;
        }

        // 工业通信协议支持的原始物理数据类型白名单
        private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            "bool", "int16", "uint16", "int32", "uint32", "float", "double", "decimal", "string"
        };

        // ========================================================
        // 核心私有工具方法：校验所属设备必须存在且未被软删除 (404 守卫)
        // ========================================================
        private async Task RequireDeviceAsync(long deviceId)
        {
            if(deviceId <= 0 || !await _repo.DeviceExistAsync(deviceId))
            {
                throw new AppException("设备不存在或者已删除", 404);
            }
        }

        // ========================================================
        // 核心私有工具方法：实体安全转换输出为 DTO (数据物理隔离)
        // ========================================================
        private static DevicePointDto ToDto(DevicePoint p) => new()
        {
            Id = p.Id,
            DeviceId = p.DeviceId,
            PointCode = p.PointCode,
            Name = p.Name,
            Address = p.Address,
            DataType = p.DataType,
            Unit = p.Unit,
            Scale = p.Scale,
            OffsetValue = p.OffsetValue,
            AccessMode = p.AccessMode,
            Enabled = p.Enabled
        };

        // ========================================================
        // 核心私有工具方法：点位与所属设备双重存在性校验 (404 守卫)
        // ========================================================
        private async Task<DevicePoint> RequirePointAsync(long id)
        {
            // 守卫第 1 道防线：查验点位自身在数据库中是否存在
            var point = await _repo.FindAsync(id) ?? throw new AppException("点位不存在", 404);

            // 守卫第 2 道防线：查验该点位归属的设备是否存在且未被软删除 (防止孤儿数据泄露)
            await RequireDeviceAsync(point.DeviceId);

            // 双重校验均通过，返回安全的点位实体
            return point;
        }

        // ========================================================
        // 核心私有工具方法：深度业务规则防线 (400 异常防御)
        // ========================================================
        private static void Validate(CreateDevicePointRequest request)
        {
            // 步骤 1：入参数据清洗（去除多余首尾空格，小写归一化）
            request.PointCode = request.PointCode?.Trim() ?? string.Empty;
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Address = request.Address?.Trim() ?? string.Empty;
            request.DataType = request.DataType?.Trim().ToLowerInvariant() ?? string.Empty;
            request.AccessMode = request.AccessMode?.Trim().ToLowerInvariant() ?? string.Empty;
            request.Unit = request.Unit?.Trim();

            // 步骤 2：长度与必填边界校验
            if (request.PointCode.Length is < 1 or > 80 || request.Name.Length is < 1 or > 100
            || request.Address.Length is < 1 or > 255 || request.Unit?.Length > 30)
            {
                throw new AppException("编码、名称、地址或单位长度不合法", 400);
            }

            // 步骤 3：数据类型白名单限制（杜绝非法工业类型）
            if (!Types.Contains(request.DataType))
            {
                throw new AppException("不支持的请求类型", 404);
            }

            // 步骤 4：访问权限模式限制（只允许 read / write / readwrite）
            if(request.AccessMode is not ("read" or "write" or "readwrite"))
            {
                throw new AppException("访问方式必须为 read、write 或 readwrite", 400);
            }

            // 步骤 5：缩放倍率严禁为 0，且倍率与偏移量最多支持 4 位小数 (对齐数据库 decimal(12,4))
            if (request.Scale == 0 || request.Scale is < -99999999.9999m or > 99999999.9999m
            || request.OffsetValue is < -99999999.9999m or > 99999999.9999m
            || decimal.Round(request.Scale, 4) != request.Scale
            || decimal.Round(request.OffsetValue, 4) != request.OffsetValue)
            {
                throw new AppException("倍率不能为0，倍率和偏移量最多4位小数且须在数据库范围内", 400);
            }
        }

        // ========================================================
        // 核心私有工具方法：将请求 DTO 字段安全复制到实体上
        // ========================================================
        private static void Apply(DevicePoint point, CreateDevicePointRequest request)
        {
            point.PointCode = request.PointCode;
            point.Name = request.Name;
            point.Address = request.Address;
            point.DataType = request.DataType;
            point.Unit = request.Unit;
            point.Scale = request.Scale;
            point.OffsetValue = request.OffsetValue;
            point.AccessMode = request.AccessMode;
            point.Enabled = request.Enabled;
            point.UpdatedAt = DateTime.UtcNow;
        } 


        /// <summary>
        /// 9.2 查询点位列表业务实现
        /// </summary>
        /// <param name="deviceId">设备id</param>
        /// <param name="Keyword"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<List<DevicePointDto>> ListAsync(long deviceId, string? Keyword)
        {
            // 第 1 步：前置设备存在性防御守卫
            await RequireDeviceAsync(deviceId);

            // 第 2 步：调用仓储查询实体列表
            var points = await _repo.ListAsync(deviceId, Keyword);

            // 第 3 步：内存投影转换为对外暴露的 DTO 列表
            return points.Select(ToDto).ToList();
        }

        /// <summary>
        /// 9.3 查询单点位详情业务实现
        /// </summary>
        /// <param name="id">点位主键 ID</param>
        /// <returns>点位详情 DTO</returns>
        public async Task<DevicePointDto> GetAsync(long id)
        {
            // 调用双重守卫拿到合法实体，并投影转换为安全 DTO
            var point = await RequirePointAsync(id);
            return ToDto(point);
        }

        /// <summary>
        /// 9.4 添加点位业务实现
        /// </summary>
        /// <param name="deviceId">所属设备主键 ID</param>
        /// <param name="request">新增点位请求参数</param>
        /// <returns>新增成功的点位 DTO（包含数据库分配的主键 ID）</returns>
        public async Task<DevicePointDto> CreateAsync(long deviceId, CreateDevicePointRequest request)
        {
            // 第 1 步：深度业务规则校验（数据类型、访问模式、倍率非零）
            Validate(request);

            // 第 2 步：父级设备存在性校验 (404 守卫)
            await RequireDeviceAsync(deviceId);

            // 第 3 步：同设备内点位编码防重校验 (409 守卫)
            if (await _repo.ExistsCodeAsync(deviceId, request.PointCode, null))
            {
                throw new AppException("同一设备内点位编码不能重复", 409);
            }

            // 第 4 步：创建全新领域实体并绑定所属设备关系
            var point = new DevicePoint
            {
                DeviceId = deviceId,
                CreatedAt = DateTime.UtcNow
            };

            // 第 5 步：调用工具方法装配清洗后的字段数据
            Apply(point, request);

            // 第 6 步：附加到仓储状态树并由工作单元提交事务持久化
            _repo.Add(point);
            await _unit.SaveChangesAsync();

            // 第 7 步：返回带有数据库生成自增主键 ID 的 DTO
            return ToDto(point);

        }

        /// <summary>
        /// 9.5 修改点位配置业务实现
        /// </summary>
        /// <param name="id">要修改的目标点位主键 ID</param>
        /// <param name="request">客户端提交的最新完整配置</param>
        /// <returns>修改成功后的最新点位 DTO</returns>
        public async Task<DevicePointDto> UpdateAsync(long id, UpdateDevicePointRequest request)
        {
            Validate(request);

            // =========================================================================
            // 【阶段 2：检查要修改的点位在不在】（查数据库点位表，不在报 404）
            // =========================================================================
            var point = await _repo.FindAsync(id);
            if(point == null)
            {
                throw new AppException("点位不存在", 404);
            }

            // =========================================================================
            // 【阶段 3：检查这个点位所属的设备在不在】（查数据库设备表，防孤儿点位，不在报 404）
            // =========================================================================
            var deviceExists = await _repo.DeviceExistAsync(point.DeviceId);
            if (!deviceExists)
            {
                throw new AppException("该点位所属的设备不存在或者已被删除", 404);
            }

            // =========================================================================
            // 【阶段 4：编码查重，关键点：排除当前正在修改的自己！】（撞名报 409）
            // =========================================================================
            // 第三个参数传入 id，告诉 SQL: 查同一个设备下有没有叫 request.PointCode 的点位，但排除 id 等于我自己的！
            var isCodeDuplicate = await _repo.ExistsCodeAsync(point.DeviceId, request.PointCode, id);
            if (isCodeDuplicate)
            {
                throw new AppException("同一设备内点位编码不能重复", 409);
            }

            // =========================================================================
            // 【阶段 5：检查全通过！把前端传来的新参数，逐行覆盖赋值给查出来的这个老实体】
            // =========================================================================
            Apply(point, request);

            // =========================================================================
            // 【阶段 6：提交工作单元，通知 MySQL 真正执行 UPDATE 语句保存入库】
            // =========================================================================
            await _unit.SaveChangesAsync();

            // 最终将修改好的实体转成 DTO 还给前端展示
            return ToDto(point);
        }

        /// <summary>
        /// 9.6 停用点位业务实现（软停用，保留点位历史配置）
        /// </summary>
        /// <param name="id">要停用的目标点位主键 ID</param>
        public async Task DisableAsync(long id)
        {
            // =========================================================================
            // 【步骤 1：检查要停用的点位在不在】（查数据库点位表，不在报 404）
            // =========================================================================
            var point = await _repo.FindAsync(id);
            if(point is null)
            {
                throw new AppException("点位不存在", 404);
            }

            // =========================================================================
            // 【步骤 2：检查这个点位所属的设备在不在】（查数据库设备表，防孤儿数据，不在报 404）
            // =========================================================================
            var deviceExists = await _repo.DeviceExistAsync(point.DeviceId);
            if (!deviceExists)
            {
                throw new AppException("该点位所属的设备不存在或已被删除", 404);
            }

            // =========================================================================
            // 【步骤 3：幂等性防御：如果当前点位本来就已经处于停用状态(0)，直接返回】
            // =========================================================================
            if(point.Enabled == 0)
            {
                return;
            }

            // =========================================================================
            // 【步骤 4：内存状态修改，将启用标记置为 0，并刷新最后修改时间】
            // =========================================================================
            point.Enabled = 0;
            point.UpdatedAt = DateTime.UtcNow;


            // =========================================================================
            // 【步骤 5：提交工作单元，由 EF Core Change Tracker 自动识别并下发 UPDATE 语句】
            // =========================================================================
            await _unit.SaveChangesAsync();
        }

        /// <summary>
        /// 9.7 恢复/启用点位业务实现
        /// </summary>
        /// <param name="id">要启用的目标点位主键 ID</param>
        public async Task EnableAsync(long id)
        {
            // =========================================================================
            // 【步骤 1：检查要启用的点位在不在】（查数据库点位表，不在报 404）
            // =========================================================================
            var point = await _repo.FindAsync(id);
            if (point is null)
            {
                throw new AppException("点位不存在", 404);
            }

            // =========================================================================
            // 【步骤 2：检查这个点位所属的设备在不在】（查数据库设备表，防孤儿数据，不在报 404）
            // =========================================================================
            var deviceExists = await _repo.DeviceExistAsync(point.DeviceId);
            if (!deviceExists)
            {
                throw new AppException("该点位所属的设备不存在或已被删除", 404);
            }

            // =========================================================================
            // 【步骤 3：幂等性防御：如果当前点位本来就已经处于启用状态(1)，直接返回】
            // =========================================================================
            if (point.Enabled == 1)
            {
                return;
            }

            // =========================================================================
            // 【步骤 4：内存状态修改，将启用标记置为 1，并刷新最后修改时间】
            // =========================================================================
            point.Enabled = 1;
            point.UpdatedAt = DateTime.UtcNow;


            // =========================================================================
            // 【步骤 5：提交工作单元，由 EF Core Change Tracker 自动识别并下发 UPDATE 语句】
            // =========================================================================
            await _unit.SaveChangesAsync();
        }

    }
}
