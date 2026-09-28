using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 保存角色请求契约（新建与更新通用）
    /// </summary>
    public class SaveRoleRequest
    {
        /// <summary>
        /// 角色编码
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 角色中文显示名称（必填，最大 100 字符）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 状态：1 启用，0 停用（默认值为 1）
        /// </summary>
        [Range(0,1)]
        public sbyte Status { get; set; }   
    }
}
