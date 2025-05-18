// OfflinePOS.DataAccess/Repositories/Repository.cs
using Microsoft.EntityFrameworkCore;
using OfflinePOS.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Repositories
{
    /// <summary>
    /// Generic repository implementation using Entity Framework Core
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// Initializes a new instance of the Repository class
        /// </summary>
        /// <param name="context">Database context</param>
        public Repository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.Where(filter).ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <inheritdoc/>
        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return true;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            return true;
        }

        /// <inheritdoc/>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> filter = null)
        {
            if (filter == null)
                return await _dbSet.CountAsync();

            return await _dbSet.Where(filter).CountAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.AnyAsync(filter);
        }
    }
}