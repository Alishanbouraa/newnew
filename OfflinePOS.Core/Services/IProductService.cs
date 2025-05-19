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
        /// Gets products by supplier ID
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <returns>Products from the supplier</returns>
        Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets products associated with a supplier invoice
        /// </summary>
        /// <param name="invoiceId">Supplier invoice ID</param>
        /// <returns>Products linked to the invoice</returns>
        Task<IEnumerable<Product>> GetProductsBySupplierInvoiceAsync(int invoiceId);

        /// <summary>
        /// Gets products with low stock
        /// </summary>
        /// <returns>Products with low stock</returns>
        Task<IEnumerable<Product>> GetLowStockProductsAsync();

        /// <summary>
        /// Generates a new unique barcode
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="barcodeType">Type of barcode (Box/Item)</param>
        /// <returns>Generated barcode</returns>
        Task<string> GenerateBarcodeAsync(int productId, string barcodeType);

        /// <summary>
        /// Updates product inventory settings
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="trackInventory">Whether to track inventory</param>
        /// <param name="allowNegativeInventory">Whether to allow negative inventory</param>
        /// <returns>Updated product</returns>
        Task<Product> UpdateInventorySettingsAsync(int productId, bool trackInventory, bool allowNegativeInventory);

        /// <summary>
        /// Imports products from a CSV file
        /// </summary>
        /// <param name="filePath">Path to CSV file</param>
        /// <param name="userId">User performing the import</param>
        /// <returns>Number of products imported</returns>
        Task<int> ImportProductsFromCsvAsync(string filePath, int userId);

        /// <summary>
        /// Exports products to a CSV file
        /// </summary>
        /// <param name="filePath">Path to save CSV file</param>
        /// <returns>Number of products exported</returns>
        Task<int> ExportProductsToCsvAsync(string filePath);

        /// <summary>
        /// Searches for products by name or barcode
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Matching products</returns>
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);

        /// <summary>
        /// Gets the stock service for product inventory management
        /// </summary>
        /// <returns>Stock service</returns>
        Task<IStockService> GetStockServiceAsync();
    }
}