// File: OfflinePOS.DataAccess/Services/CategoryService.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Services
{
    /// <summary>
    /// Service for managing product categories
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryService> _logger;

        /// <summary>
        /// Initializes a new instance of the CategoryService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            try
            {
                return await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all categories");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Category>> GetCategoriesByTypeAsync(string type)
        {
            try
            {
                return await _unitOfWork.Categories.GetAsync(c => c.IsActive && c.Type == type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories by type {Type}", type);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Categories.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category by ID {CategoryId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Category> CreateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            try
            {
                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category created: {CategoryName}", category.Name);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category {CategoryName}", category.Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            try
            {
                var existingCategory = await _unitOfWork.Categories.GetByIdAsync(category.Id);
                if (existingCategory == null)
                    return false;

                await _unitOfWork.Categories.UpdateAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category updated: {CategoryName}", category.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", category.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                    return false;

                // Soft delete - set IsActive to false
                category.IsActive = false;
                await _unitOfWork.Categories.UpdateAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category soft deleted: {CategoryId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                throw;
            }
        }
    }
}