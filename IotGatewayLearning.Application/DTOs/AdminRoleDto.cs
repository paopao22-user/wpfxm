using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 后台角色信息展示 DTO（向前端输出的数据传输对象）
    /// </summary>
    public class AdminRoleDto
    {
        /// <summary>
        /// 角色主键自增 ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 角色业务唯一编码（例如：admin, operator, engineer）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 角色显示名称（例如：系统管理员, 操作员, 工程师）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色状态：1 启用，0 停用
        /// </summary>
        public sbyte Status { get; set; }   
    }
}
