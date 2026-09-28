using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 权限目录脱敏信息出参契约（用于列表展现与详情回显）
    /// </summary>
    public class AdminPermissionDto
    {
        /// <summary>
        /// 权限主键自增 ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 权限标准编码（如 device:read, user:manage）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 权限中文名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限业务描述与说明（可为空）
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 状态：1 启用，0 停用
        /// </summary>
        public sbyte Status { get; set; } = 1;
    }
}
