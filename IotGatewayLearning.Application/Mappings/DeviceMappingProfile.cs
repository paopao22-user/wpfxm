using AutoMapper;
using IotGatewayLearning.Application.DTOs.Devices.Requests;
using IotGatewayLearning.Application.DTOs.Devices.Responses;
using IotGatewayLearning.Domain.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.Mappings
{
    /// <summary>
    /// 设备台账领域模型与 DTO 映射配置文件
    /// </summary>
    public class DeviceMappingProfile:Profile
    {
        public DeviceMappingProfile()
        {
            // ==========================================
            // 1. 新增请求 -> 设备实体 (Request -> Entity)
            // ==========================================
            // 强制忽略所有系统内部维护字段，只拷贝前端允许提交的业务数据
            //CreateMap<CreateDeviceRequest, Device>():AutoMapper 基类 Profile 提供的方法，意思是“在内存中注册一对类型之间的转换图纸”
            //左边source 数据源,右边target 目标. 只要属性名字相同、类型兼容（例如 Name 对应 Name），AutoMapper 默认会自动帮你赋值，根本不需要手工写
            //ForMember(...)：英文直译就是“对于目标对象的某一个成员属性”。它表示：“对于右边 Device 实体的这个属性，我要单独定特殊规矩，不按默认的来！”
            //忽略的属性都是：“前端请求里没有、但数据库实体里必须有”的内部系统属性！
            CreateMap<CreateDeviceRequest, Device>()
                .ForMember(x => x.Id, options => options.Ignore())
                .ForMember(x => x.Status, options => options.Ignore())
                .ForMember(x => x.IsDeleted, options => options.Ignore())
                .ForMember(x => x.ConnectionStatus, options => options.Ignore())
                .ForMember(x => x.RunStatus, options => options.Ignore())
                .ForMember(x => x.Points, options => options.Ignore())
                .ForMember(x => x.CreatedAt, options => options.Ignore())
                .ForMember(x => x.UpdatedAt, options => options.Ignore());


            // ==========================================
            // 2. 修改请求 -> 设备实体 (Request -> Entity)
            // ==========================================
            // 用于把前端修改的数据合并进已有的数据库实体中
            CreateMap<UpdateDeviceRequest, Device>()
                .ForMember(x => x.Id, options => options.Ignore())
                .ForMember(x => x.Status, options => options.Ignore())
                .ForMember(x => x.IsDeleted, options => options.Ignore())
                .ForMember(x => x.ConnectionStatus, options => options.Ignore())
                .ForMember(x => x.RunStatus, options => options.Ignore())
                .ForMember(x => x.CreatedAt, options => options.Ignore())
                .ForMember(x => x.UpdatedAt, options => options.Ignore())
                .ForMember(x => x.Points, options => options.Ignore());


            // ==========================================
            // 3. 设备实体 -> 列表 DTO (Entity -> ListItemDto)
            // ==========================================
            // 同名字段全自动匹配，绝不挂载点位子表，保持极速查询
            CreateMap<Device, DeviceListItemDto>();


            // ==========================================
            // 4. 点位实体 -> 点位 DTO (Entity -> PointDto)
            // ==========================================
            CreateMap<DevicePoint, DevicePointDto>();

            // ==========================================
            // 5. 设备实体 -> 详情 DTO (Entity -> DetailDto)
            // ==========================================
            // Points 集合由于在两边名字相同，AutoMapper 会自动调用上面的规则将点位实体转换为点位 DTO！
            CreateMap<Device, DeviceDetailDto>();

        }
    }
}
