// File: OfflinePOS.Core/Services/ICategoryService.cs
using OfflinePOS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing product categories
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Gets all active categories
        /// </summary>
        /// <returns>List of active categories</returns>
        Task<IEnumerable<Category>> GetAllCategoriesAsync();

        /// <summary>
        /// Gets categories by type (Product/Expense)
        /// </summary>
        /// <param name="type">Category type</param>
        /// <returns>List of categories of the specified type</returns>
        Task<IEnumerable<Category>> GetCategoriesByTypeAsync(string type);

        /// <summary>
        /// Gets a category by ID
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category if found, null otherwise</returns>
        Task<Category> GetCategoryByIdAsync(int id);

        /// <summary>
        /// Creates a new category
        /// </summary>
        /// <param name="category">Category to create</param>
        /// <returns>Created category</returns>
        Task<Category> CreateCategoryAsync(Category category);

        /// <summary>
        /// Updates an existing category
        /// </summary>
        /// <param name="category">Category to update</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateCategoryAsync(Category category);

        /// <summary>
        /// Deletes a category by ID
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteCategoryAsync(int id);
    }
}