using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 保存权限请求契约（新增与修改共用入参模型）:前端输入的表单
    /// </summary>
    public class SavePermissionRequest
    {
        /// <summary>
        /// 权限编码（必填，最大 100 字符）
        /// </summary>
        [Required(ErrorMessage = "权限编码不能为空")]
        [StringLength(100, ErrorMessage = "权限编码长度不能超过 100 个字符")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 权限中文名称（必填，最大 100 字符）
        /// </summary>
        [Required(ErrorMessage = "权限名称不能为空")]
        [StringLength(100, ErrorMessage = "权限名称长度不能超过 100 个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限详细描述说明（选填，最大 255 字符）
        /// </summary>
        /// 
        [StringLength(255, ErrorMessage = "权限描述长度不能超过 255 个字符")]
        public string? Description { get; set; }


        /// <summary>
        /// 状态：1 启用，0 停用（默认值为 1）
        /// </summary>
        public sbyte Status { get; set; } = 1;
    }
}
