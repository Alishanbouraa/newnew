// OfflinePOS.Core/Services/IProductService.cs
using OfflinePOS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing products
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Gets all active products
        /// </summary>
        /// <returns>List of active products</returns>
        Task<IEnumerable<Product>> GetAllProductsAsync();

        /// <summary>
        /// Gets all products in a specific category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Products in the category</returns>
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);

        /// <summary>
        /// Gets a product by its ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product if found, null otherwise</returns>
        Task<Product> GetProductByIdAsync(int id);

        /// <summary>
        /// Gets a product by its barcode
        /// </summary>
        /// <param name="barcode">Box or item barcode</param>
        /// <returns>Product if found, null otherwise</returns>
        Task<Product> GetProductByBarcodeAsync(string barcode);

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <param name="product">Product to create</param>
        /// <returns>Created product</returns>
        Task<Product> CreateProductAsync(Product product);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateProductAsync(Product product);

        /// <summary>
        /// Deletes a product by ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteProductAsync(int id);

        /// <summary>
        /// Searches for products by name or barcode
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Matching products</returns>
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
    }
}