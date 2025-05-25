// OfflinePOS.Core/Repositories/IRepository.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Repositories
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <returns>All entities</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Gets entities based on a filter expression
        /// </summary>
        /// <param name="filter">Filter expression</param>
        /// <returns>Filtered entities</returns>
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter);

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Entity if found, null otherwise</returns>
        Task<T> GetByIdAsync(int id);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <returns>Added entity</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <returns>True if updated, false otherwise</returns>
        Task<bool> UpdateAsync(T entity);
     
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Gets count of entities based on filter
        /// </summary>
        /// <param name="filter">Filter expression</param>
        /// <returns>Count of entities</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> filter = null);

        /// <summary>
        /// Checks if any entity matches the filter
        /// </summary>
        /// <param name="filter">Filter expression</param>
        /// <returns>True if any match found, false otherwise</returns>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
    }
}