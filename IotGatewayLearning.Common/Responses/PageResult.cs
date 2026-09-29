using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Common.Responses
{
    /// <summary>
    /// 通用分页数据包装模型（支持任意业务实体的分页承载）
    /// </summary>
    /// <typeparam name="T">列表项数据类型</typeparam>
    public class PageResult<T>
    {
        /// <summary>
        /// 当前页的数据列表
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// 符合条件的总记录数（由 COUNT(*) 产生）
        /// </summary>
        public long  Total { get; set; }

        /// <summary>
        /// 当前页码（从 1 开始）
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每页显示记录条数
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 计算属性：总页数（自动向上取整）
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    }
}
