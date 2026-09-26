using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    /// <summary>
    /// 通用泛型仓储接口（封装对任意数据库实体 TEntity 的基础增删改查契约）
    /// </summary>
    /// <typeparam name="TEntity">数据库表对应的实体模型类型（必须为引用类型 class）</typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// 根据主键 ID 异步查询单个实体
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntity?> GetByIdAsync(long id);

        /// <summary>
        /// 异步获取当前表中的所有实体记录列表
        /// </summary>
        /// <returns></returns>
        Task<List<TEntity>> GetAllAsync();

        /// <summary>
        /// 根据指定的 Lambda 条件表达式，异步查询满足条件的第一个实体
        /// </summary>
        /// <param name="predicate">筛选条件表达式树（例如：u => u.UserName == "admin"）</param>
        /// <returns>返回第一个匹配的实体对象；若没有任何匹配项则返回 null</returns>
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 根据指定的 Lambda 条件表达式，异步判断数据库中是否存在满足条件的记录
        /// </summary>
        /// <param name="predicate">筛选条件表达式树（例如：u => u.UserName == "admin"）</param>
        /// <returns>若存在至少一条匹配记录则返回 true；否则返回 false</returns>
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 异步将新实体添加到 EF Core 变更追踪器中（状态标记为 Added，需调用 SaveChangesAsync 提交入库）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task AddAsync(TEntity entity);

        /// <summary>
        /// 将实体的状态在 EF Core 变更追踪器中标记为已修改（Modified，需调用 SaveChangesAsync 提交入库）
        /// </summary>
        /// <param name="entity"></param>
        void Update(TEntity entity);

        /// <summary>
        /// 将实体的状态在 EF Core 变更追踪器中标记为已删除（Deleted，需调用 SaveChangesAsync 提交入库
        /// </summary>
        /// <param name="entity"></param>
        void Delete(TEntity entity);
    }
}
