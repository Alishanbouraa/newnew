// OfflinePOS.DataAccess/Services/ProductService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Services
{
    /// <summary>
    /// Service for managing products
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        /// <summary>
        /// Initializes a new instance of the ProductService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _unitOfWork.Products.GetAsync(p => p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                return await _unitOfWork.Products.GetAsync(p => p.CategoryId == categoryId && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category {CategoryId}", categoryId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Product> GetProductByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Products.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID {ProductId}", id);
                throw;
            }
        }
        // OfflinePOS.DataAccess/Services/ProductService.cs - Add these methods to your class

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId)
        {
            try
            {
                return await _unitOfWork.Products.GetAsync(p => p.SupplierId == supplierId && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by supplier {SupplierId}", supplierId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            try
            {
                // This requires joining with Stock table, so we'll use the StockService
                var stockService = new StockService(_unitOfWork, _logger);
                var lowStockItems = await stockService.GetLowStockProductsAsync();
                return lowStockItems.Select(item => item.Product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock products");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateBarcodeAsync(int productId, string barcodeType)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {productId} not found");

                // Generate barcode using utility
                string barcode = Core.Utilities.BarcodeUtility.GenerateEAN13(productId);

                // Update the product with the new barcode
                if (barcodeType.Equals("Box", StringComparison.OrdinalIgnoreCase))
                {
                    product.BoxBarcode = barcode;
                }
                else
                {
                    product.ItemBarcode = barcode;
                }

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                return barcode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating barcode for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Product> UpdateInventorySettingsAsync(
            int productId,
            bool trackInventory,
            bool allowNegativeInventory)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {productId} not found");

                product.TrackInventory = trackInventory;
                product.AllowNegativeInventory = allowNegativeInventory;

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory settings for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ImportProductsFromCsvAsync(string filePath, int userId)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("CSV file not found", filePath);

                int importCount = 0;
                using (var reader = new StreamReader(filePath))
                {
                    // Skip header row
                    var headerLine = await reader.ReadLineAsync();
                    if (headerLine == null)
                        return 0;

                    // Parse headers
                    var headers = headerLine.Split(',');

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var values = line.Split(',');
                        if (values.Length < headers.Length)
                            continue;

                        try
                        {
                            // Create a dictionary for easier mapping
                            var data = new Dictionary<string, string>();
                            for (int i = 0; i < headers.Length; i++)
                            {
                                data[headers[i].Trim()] = values[i].Trim();
                            }

                            // Parse product data
                            var product = new Product
                            {
                                Name = GetStringValue(data, "Name"),
                                Description = GetStringValue(data, "Description", ""),
                                CategoryId = GetIntValue(data, "CategoryId", 1),
                                BoxBarcode = GetStringValue(data, "BoxBarcode", ""),
                                ItemBarcode = GetStringValue(data, "ItemBarcode", ""),
                                ItemsPerBox = GetIntValue(data, "ItemsPerBox", 1),
                                BoxPurchasePrice = GetDecimalValue(data, "BoxPurchasePrice", 0),
                                BoxWholesalePrice = GetDecimalValue(data, "BoxWholesalePrice", 0),
                                BoxSalePrice = GetDecimalValue(data, "BoxSalePrice", 0),
                                ItemPurchasePrice = GetDecimalValue(data, "ItemPurchasePrice", 0),
                                ItemWholesalePrice = GetDecimalValue(data, "ItemWholesalePrice", 0),
                                ItemSalePrice = GetDecimalValue(data, "ItemSalePrice", 0),
                                SupplierId = GetIntValue(data, "SupplierId", null),
                                SupplierProductCode = GetStringValue(data, "SupplierProductCode", ""),
                                TrackInventory = GetBoolValue(data, "TrackInventory", true),
                                AllowNegativeInventory = GetBoolValue(data, "AllowNegativeInventory", false),
                                Weight = GetDecimalValue(data, "Weight", null),
                                Dimensions = GetStringValue(data, "Dimensions", ""),
                                CreatedById = userId,
                                IsActive = true
                            };

                            // Generate barcodes if not provided
                            if (string.IsNullOrEmpty(product.BoxBarcode))
                            {
                                product.BoxBarcode = Core.Utilities.BarcodeUtility.GenerateEAN13(DateTime.Now.Millisecond + importCount);
                            }

                            if (string.IsNullOrEmpty(product.ItemBarcode))
                            {
                                product.ItemBarcode = Core.Utilities.BarcodeUtility.GenerateEAN13(DateTime.Now.Millisecond + importCount + 1000);
                            }

                            // Add product
                            await _unitOfWork.Products.AddAsync(product);
                            await _unitOfWork.SaveChangesAsync();

                            // Create stock record
                            int boxQuantity = GetIntValue(data, "BoxQuantity", 0);
                            int itemQuantity = GetIntValue(data, "ItemQuantity", 0);

                            if (product.TrackInventory && (boxQuantity > 0 || itemQuantity > 0))
                            {
                                var stock = new Stock
                                {
                                    ProductId = product.Id,
                                    BoxQuantity = boxQuantity,
                                    ItemQuantity = itemQuantity,
                                    MinimumBoxLevel = GetIntValue(data, "MinimumBoxLevel", 1),
                                    MinimumItemLevel = GetIntValue(data, "MinimumItemLevel", product.ItemsPerBox / 2),
                                    ReorderBoxQuantity = GetIntValue(data, "ReorderBoxQuantity", 1),
                                    LocationCode = GetStringValue(data, "LocationCode", ""),
                                    StockStatus = "Available",
                                    CreatedById = userId
                                };

                                await _unitOfWork.Stocks.AddAsync(stock);
                                await _unitOfWork.SaveChangesAsync();

                                // Create stock adjustment record
                                var adjustment = new StockAdjustment
                                {
                                    ProductId = product.Id,
                                    AdjustmentType = "Addition",
                                    BoxQuantity = boxQuantity,
                                    ItemQuantity = itemQuantity,
                                    PreviousBoxQuantity = 0,
                                    PreviousItemQuantity = 0,
                                    NewBoxQuantity = boxQuantity,
                                    NewItemQuantity = itemQuantity,
                                    ReferenceNumber = $"IMPORT-{DateTime.Now:yyyyMMdd}",
                                    Reason = "Initial import",
                                    AdjustmentDate = DateTime.Now,
                                    CreatedById = userId
                                };

                                await _unitOfWork.StockAdjustments.AddAsync(adjustment);
                                await _unitOfWork.SaveChangesAsync();
                            }

                            importCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error importing product from CSV row: {Row}", line);
                            // Continue with next row
                        }
                    }
                }

                return importCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from CSV");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ExportProductsToCsvAsync(string filePath)
        {
            try
            {
                var products = await _unitOfWork.Products.GetAsync(p => p.IsActive);

                using (var writer = new StreamWriter(filePath))
                {
                    // Write header
                    await writer.WriteLineAsync("Id,Name,Description,CategoryId,BoxBarcode,ItemBarcode,ItemsPerBox," +
                        "BoxPurchasePrice,BoxWholesalePrice,BoxSalePrice,ItemPurchasePrice,ItemWholesalePrice,ItemSalePrice," +
                        "SupplierId,SupplierProductCode,TrackInventory,AllowNegativeInventory,Weight,Dimensions");

                    // Write products
                    foreach (var product in products)
                    {
                        await writer.WriteLineAsync(
                            $"{product.Id}," +
                            $"\"{EscapeCsvField(product.Name)}\"," +
                            $"\"{EscapeCsvField(product.Description)}\"," +
                            $"{product.CategoryId}," +
                            $"{product.BoxBarcode}," +
                            $"{product.ItemBarcode}," +
                            $"{product.ItemsPerBox}," +
                            $"{product.BoxPurchasePrice}," +
                            $"{product.BoxWholesalePrice}," +
                            $"{product.BoxSalePrice}," +
                            $"{product.ItemPurchasePrice}," +
                            $"{product.ItemWholesalePrice}," +
                            $"{product.ItemSalePrice}," +
                            $"{product.SupplierId}," +
                            $"\"{EscapeCsvField(product.SupplierProductCode)}\"," +
                            $"{product.TrackInventory}," +
                            $"{product.AllowNegativeInventory}," +
                            $"{product.Weight}," +
                            $"\"{EscapeCsvField(product.Dimensions)}\""
                        );
                    }
                }

                return products.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products to CSV");
                throw;
            }
        }

        // Helper methods for CSV import/export
        private string GetStringValue(Dictionary<string, string> data, string key, string defaultValue = null)
        {
            if (data.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
                return value;
            return defaultValue;
        }

        private int GetIntValue(Dictionary<string, string> data, string key, int? defaultValue = null)
        {
            if (data.TryGetValue(key, out string value) && int.TryParse(value, out int intValue))
                return intValue;
            return defaultValue ?? 0;
        }

        private decimal GetDecimalValue(Dictionary<string, string> data, string key, decimal? defaultValue = null)
        {
            if (data.TryGetValue(key, out string value) && decimal.TryParse(value, out decimal decimalValue))
                return decimalValue;
            return defaultValue ?? 0;
        }

        private bool GetBoolValue(Dictionary<string, string> data, string key, bool defaultValue = false)
        {
            if (data.TryGetValue(key, out string value) && bool.TryParse(value, out bool boolValue))
                return boolValue;
            return defaultValue;
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // Escape quotes with double quotes
            return field.Replace("\"", "\"\"");
        }
        /// <inheritdoc/>
        public async Task<Product> GetProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return null;

            try
            {
                var products = await _unitOfWork.Products.GetAsync(
                    p => (p.BoxBarcode == barcode || p.ItemBarcode == barcode) && p.IsActive);

                return products.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by barcode {Barcode}", barcode);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Product> CreateProductAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            try
            {
                // Check if product with the same barcodes already exists
                if (!string.IsNullOrEmpty(product.BoxBarcode))
                {
                    var existingByBoxBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.BoxBarcode == product.BoxBarcode && p.IsActive);

                    if (existingByBoxBarcode)
                        throw new InvalidOperationException($"A product with box barcode {product.BoxBarcode} already exists");
                }

                if (!string.IsNullOrEmpty(product.ItemBarcode))
                {
                    var existingByItemBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.ItemBarcode == product.ItemBarcode && p.IsActive);

                    if (existingByItemBarcode)
                        throw new InvalidOperationException($"A product with item barcode {product.ItemBarcode} already exists");
                }

                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product created: {ProductName}", product.Name);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", product.Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateProductAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            try
            {
                // Check if product exists
                var existingProduct = await _unitOfWork.Products.GetByIdAsync(product.Id);
                if (existingProduct == null)
                    return false;

                // Check if barcodes are unique
                if (!string.IsNullOrEmpty(product.BoxBarcode))
                {
                    var existingByBoxBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.BoxBarcode == product.BoxBarcode && p.Id != product.Id && p.IsActive);

                    if (existingByBoxBarcode)
                        throw new InvalidOperationException($"Another product with box barcode {product.BoxBarcode} already exists");
                }

                if (!string.IsNullOrEmpty(product.ItemBarcode))
                {
                    var existingByItemBarcode = await _unitOfWork.Products.ExistsAsync(
                        p => p.ItemBarcode == product.ItemBarcode && p.Id != product.Id && p.IsActive);

                    if (existingByItemBarcode)
                        throw new InvalidOperationException($"Another product with item barcode {product.ItemBarcode} already exists");
                }

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product updated: {ProductName}", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                    return false;

                // Soft delete - set IsActive to false
                product.IsActive = false;
                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product soft deleted: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Enumerable.Empty<Product>();

            try
            {
                var products = await _unitOfWork.Products.GetAsync(
                    p => (p.Name.Contains(searchTerm) ||
                          p.BoxBarcode == searchTerm ||
                          p.ItemBarcode == searchTerm) &&
                          p.IsActive);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}