// OfflinePOS.Core/Services/IProductService.cs
using OfflinePOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Comprehensive service interface for managing products with advanced inventory management capabilities
    /// </summary>
    public interface IProductService
    {
        #region Core Product Management

        /// <summary>
        /// Gets all active products regardless of availability status
        /// </summary>
        /// <returns>List of all active products</returns>
        Task<IEnumerable<Product>> GetAllProductsAsync();

        /// <summary>
        /// Gets products by category regardless of availability status
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Products in the specified category</returns>
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);

        /// <summary>
        /// Gets a product by its unique identifier
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product if found, null otherwise</returns>
        Task<Product> GetProductByIdAsync(int id);

        /// <summary>
        /// Gets a product by its barcode (box or item)
        /// </summary>
        /// <param name="barcode">Box or item barcode</param>
        /// <returns>Product if found, null otherwise</returns>
        Task<Product> GetProductByBarcodeAsync(string barcode);

        /// <summary>
        /// Creates a new product (automatically placed in inventory)
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
        /// Soft deletes a product by setting IsActive to false
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteProductAsync(int id);

        /// <summary>
        /// Searches for products by name or barcode across all products
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Matching products</returns>
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);

        #endregion

        #region Enhanced Inventory Management

        /// <summary>
        /// Gets all products in inventory (not available for sale)
        /// These are products that have been purchased/received but not yet made available to customers
        /// </summary>
        /// <returns>Products in inventory storage</returns>
        Task<IEnumerable<Product>> GetInventoryProductsAsync();

        /// <summary>
        /// Gets all products in the catalog (available for sale)
        /// These are products that customers can purchase
        /// </summary>
        /// <returns>Products available in catalog</returns>
        Task<IEnumerable<Product>> GetCatalogProductsAsync();

        /// <summary>
        /// Gets inventory products filtered by category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Inventory products in the specified category</returns>
        Task<IEnumerable<Product>> GetInventoryProductsByCategoryAsync(int categoryId);

        /// <summary>
        /// Gets catalog products filtered by category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Catalog products in the specified category</returns>
        Task<IEnumerable<Product>> GetCatalogProductsByCategoryAsync(int categoryId);

        /// <summary>
        /// Transfers a product from inventory to catalog (makes it available for sale)
        /// </summary>
        /// <param name="productId">Product ID to transfer</param>
        /// <param name="userId">User performing the transfer</param>
        /// <returns>Updated product</returns>
        Task<Product> TransferToCatalogAsync(int productId, int userId);

        /// <summary>
        /// Transfers a product from catalog back to inventory (removes from sale)
        /// </summary>
        /// <param name="productId">Product ID to transfer</param>
        /// <param name="userId">User performing the transfer</param>
        /// <returns>Updated product</returns>
        Task<Product> TransferToInventoryAsync(int productId, int userId);

        /// <summary>
        /// Transfers multiple products from inventory to catalog in a single operation
        /// </summary>
        /// <param name="productIds">Collection of product IDs to transfer</param>
        /// <param name="userId">User performing the bulk transfer</param>
        /// <returns>Number of products successfully transferred</returns>
        Task<int> BulkTransferToCatalogAsync(IEnumerable<int> productIds, int userId);

        /// <summary>
        /// Transfers multiple products from catalog back to inventory in a single operation
        /// </summary>
        /// <param name="productIds">Collection of product IDs to transfer</param>
        /// <param name="userId">User performing the bulk transfer</param>
        /// <returns>Number of products successfully transferred</returns>
        Task<int> BulkTransferToInventoryAsync(IEnumerable<int> productIds, int userId);

        /// <summary>
        /// Gets products that are ready for catalog transfer (have sufficient stock)
        /// </summary>
        /// <returns>Products ready to be made available for sale</returns>
        Task<IEnumerable<Product>> GetProductsReadyForCatalogAsync();

        /// <summary>
        /// Searches inventory products by name, barcode, or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Matching inventory products</returns>
        Task<IEnumerable<Product>> SearchInventoryProductsAsync(string searchTerm);

        /// <summary>
        /// Searches catalog products by name, barcode, or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Matching catalog products</returns>
        Task<IEnumerable<Product>> SearchCatalogProductsAsync(string searchTerm);

        /// <summary>
        /// Gets comprehensive inventory statistics
        /// </summary>
        /// <returns>Detailed inventory statistics</returns>
        Task<InventoryStatistics> GetInventoryStatisticsAsync();

        #endregion

        #region Supplier and Invoice Management

        /// <summary>
        /// Gets products by supplier ID
        /// </summary>
        /// <param name="supplierId">Supplier ID</param>
        /// <returns>Products from the specified supplier</returns>
        Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets products associated with a specific supplier invoice
        /// </summary>
        /// <param name="invoiceId">Supplier invoice ID</param>
        /// <returns>Products linked to the invoice</returns>
        Task<IEnumerable<Product>> GetProductsBySupplierInvoiceAsync(int invoiceId);

        #endregion

        #region Stock and Inventory Control

        /// <summary>
        /// Gets products with low stock levels that need reordering
        /// </summary>
        /// <returns>Products with low or out of stock</returns>
        Task<IEnumerable<Product>> GetLowStockProductsAsync();

        /// <summary>
        /// Updates inventory tracking settings for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="trackInventory">Whether to track inventory for this product</param>
        /// <param name="allowNegativeInventory">Whether to allow negative inventory</param>
        /// <returns>Updated product</returns>
        Task<Product> UpdateInventorySettingsAsync(int productId, bool trackInventory, bool allowNegativeInventory);

        #endregion

        #region Barcode Management

        /// <summary>
        /// Generates a new unique barcode for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="barcodeType">Type of barcode to generate (Box/Item)</param>
        /// <returns>Generated barcode string</returns>
        Task<string> GenerateBarcodeAsync(int productId, string barcodeType);

        #endregion

        #region Data Import/Export

        /// <summary>
        /// Imports products from a CSV file
        /// Products imported via CSV are automatically placed in inventory
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="userId">User performing the import</param>
        /// <returns>Number of products successfully imported</returns>
        Task<int> ImportProductsFromCsvAsync(string filePath, int userId);

        /// <summary>
        /// Exports products to a CSV file
        /// </summary>
        /// <param name="filePath">Path where CSV file will be saved</param>
        /// <returns>Number of products exported</returns>
        Task<int> ExportProductsToCsvAsync(string filePath);

        #endregion

        #region Service Integration

        /// <summary>
        /// Gets the stock service for advanced inventory operations
        /// NOTE: This method is deprecated to prevent DbContext sharing issues
        /// </summary>
        /// <returns>Stock service instance (deprecated)</returns>
        [Obsolete("This method is deprecated to prevent DbContext sharing issues. Use IStockService directly within the same service scope.")]
        Task<IStockService> GetStockServiceAsync();

        #endregion
    }

    /// <summary>
    /// Comprehensive inventory statistics model providing insights into inventory and catalog status
    /// </summary>
    public class InventoryStatistics
    {
        /// <summary>
        /// Total number of products in inventory (not available for sale)
        /// </summary>
        public int TotalInventoryProducts { get; set; }

        /// <summary>
        /// Total number of products in catalog (available for sale)
        /// </summary>
        public int TotalCatalogProducts { get; set; }

        /// <summary>
        /// Number of inventory products that are ready to be transferred to catalog
        /// (have sufficient stock and meet transfer criteria)
        /// </summary>
        public int ProductsReadyForCatalog { get; set; }

        /// <summary>
        /// Number of products with low stock levels requiring attention
        /// </summary>
        public int LowStockProducts { get; set; }

        /// <summary>
        /// Total value of inventory products based on purchase prices
        /// </summary>
        public decimal TotalInventoryValue { get; set; }

        /// <summary>
        /// Total potential revenue value of catalog products based on sale prices
        /// </summary>
        public decimal TotalCatalogValue { get; set; }

        /// <summary>
        /// Number of unique categories represented in inventory
        /// </summary>
        public int InventoryCategories { get; set; }

        /// <summary>
        /// Number of unique categories represented in catalog
        /// </summary>
        public int CatalogCategories { get; set; }

        /// <summary>
        /// Number of unique suppliers for inventory products
        /// </summary>
        public int UniqueSuppliers { get; set; }

        /// <summary>
        /// Average time products spend in inventory before being transferred to catalog
        /// </summary>
        public TimeSpan? AverageInventoryDuration { get; set; }

        /// <summary>
        /// Date and time when these statistics were calculated
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }
}