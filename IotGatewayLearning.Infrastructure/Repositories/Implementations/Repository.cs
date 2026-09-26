using IotGatewayLearning.Infrastructure.Data;
using IotGatewayLearning.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.Infrastructure.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext _context;  //数据库上下文

        protected readonly DbSet<TEntity> _dbSet;  //实体数据集


        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();    
        }
        public async Task AddAsync(TEntity entity)
        {
             await _dbSet.AddAsync(entity);
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate)
        {
            // _dbSet.Where(predicate) 并没有真正查库，它只是在拼装 IQueryable 查询表达式
            // 直到 await ToListAsync() 时，EF Core 才把整棵树翻译为 SQL 发给 MySQL 执行
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<List<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }
    }
}
