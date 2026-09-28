using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 权限树节点数据传输模型（自引用树形结构）
    /// </summary>
    public class PermissionNodeDto
    {
        /// <summary>
        /// 节点唯一标识 Key（父分组如 "group:device"，叶子权限如 "perm:1"）
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 节点在界面上显示的中文标签（如 "设备管理" 或 "查看设备 (device:read)"）
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 真实权限主键 ID（如果是父分组则为 null；叶子节点为数据库自增 ID）
        /// </summary>
        public long? PermissionId { get; set; }

        /// <summary>
        /// 权限标准编码（父分组可为空或模块名；叶子节点为 device:read）
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 标记是否是分组父节点（true: 分类文件夹; false: 真实可授权权限）
        /// </summary>
        public bool IsGroup { get; set; }

        /// <summary>
        /// 下级子节点集合（自引用递归属性）
        /// </summary>
        public List<PermissionNodeDto> Children { get; set; } = new();
    }
}
