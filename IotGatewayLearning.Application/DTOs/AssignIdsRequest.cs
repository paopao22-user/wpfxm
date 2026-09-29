using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Application.DTOs
{
    /// <summary>
    /// 分配关联主键 ID 集合请求 DTO（阶段 11 用户分配角色与阶段 12 角色分配权限共用）
    /// </summary>
    public sealed class AssignIdsRequest
    {
        /// <summary>
        /// 期望最终绑定的目标 ID 集合（空数组 [] 表示清空所有关联；null 属于非法参数）
        /// </summary>
        [Required(ErrorMessage = "授权 ID 集合不能为 null")]
        public List<long> Ids { get; set; } = new List<long>();
    }
}
