// File: OfflinePOS.DataAccess/Services/ProductService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using OfflinePOS.DataAccess.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace OfflinePOS.DataAccess.Services
{
    /// <summary>
    /// Service for managing products with proper DbContext lifecycle management
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
                // Set default values for optional fields
                if (string.IsNullOrEmpty(product.SupplierProductCode))
                    product.SupplierProductCode = string.Empty;

                // Validate required barcodes
                if (string.IsNullOrEmpty(product.BoxBarcode) || string.IsNullOrEmpty(product.ItemBarcode))
                {
                    throw new InvalidOperationException("Box and Item barcodes are required");
                }

                // Check for duplicate barcodes
                await ValidateUniqueBarcodes(product);

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

                // Validate unique barcodes (excluding this product)
                await ValidateUniqueBarcodes(product);

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
        public async Task<IEnumerable<Product>> GetProductsBySupplierInvoiceAsync(int invoiceId)
        {
            try
            {
                var products = await _unitOfWork.Products.GetAsync(
                    p => p.SupplierInvoiceId == invoiceId && p.IsActive);

                var results = new List<Product>();
                foreach (var product in products)
                {
                    if (product.SupplierId.HasValue)
                    {
                        product.Supplier = await _unitOfWork.Suppliers.GetByIdAsync(product.SupplierId.Value);
                    }
                    results.Add(product);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by supplier invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            try
            {
                var stockServiceLogger = new LoggerAdapter<StockService>(_logger);
                var stockService = new StockService(_unitOfWork, stockServiceLogger);
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

                string barcode = Core.Utilities.BarcodeUtility.GenerateEAN13(productId);

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
                    var headerLine = await reader.ReadLineAsync();
                    if (headerLine == null)
                        return 0;

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
                            var data = CreateDataDictionary(headers, values);
                            var product = CreateProductFromData(data, userId);

                            await _unitOfWork.Products.AddAsync(product);
                            await _unitOfWork.SaveChangesAsync();

                            await CreateInitialStockForImport(product, data, userId);
                            importCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error importing product from CSV row: {Row}", line);
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
                    await writer.WriteLineAsync(CreateCsvHeader());

                    foreach (var product in products)
                    {
                        await writer.WriteLineAsync(CreateCsvRow(product));
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

        /// <inheritdoc/>
        /// <summary>
        /// DEPRECATED: This method is deprecated to prevent DbContext sharing issues.
        /// Use IStockService directly within the same service scope instead.
        /// </summary>
        public async Task<IStockService> GetStockServiceAsync()
        {
            _logger.LogWarning("GetStockServiceAsync is deprecated. Create StockService within the same service scope.");
            return null;
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates that barcodes are unique across all products
        /// </summary>
        /// <param name="product">Product to validate</param>
        private async Task ValidateUniqueBarcodes(Product product)
        {
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
        }

        /// <summary>
        /// Creates a data dictionary from CSV headers and values
        /// </summary>
        private Dictionary<string, string> CreateDataDictionary(string[] headers, string[] values)
        {
            var data = new Dictionary<string, string>();
            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                data[headers[i].Trim()] = values[i].Trim();
            }
            return data;
        }

        /// <summary>
        /// Creates a product from CSV data
        /// </summary>
        private Product CreateProductFromData(Dictionary<string, string> data, int userId)
        {
            var product = new Product
            {
                Name = GetStringValue(data, "Name"),
                Description = GetStringValue(data, "Description", null),
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
                SupplierInvoiceId = GetIntValue(data, "SupplierInvoiceId", null),
                SupplierProductCode = GetStringValue(data, "SupplierProductCode", ""),
                TrackInventory = GetBoolValue(data, "TrackInventory", true),
                AllowNegativeInventory = GetBoolValue(data, "AllowNegativeInventory", false),
                Weight = GetDecimalValue(data, "Weight", null),
                Dimensions = GetStringValue(data, "Dimensions", null),
                CreatedById = userId,
                IsActive = true
            };

            // Generate barcodes if not provided
            if (string.IsNullOrEmpty(product.BoxBarcode))
            {
                product.BoxBarcode = Core.Utilities.BarcodeUtility.GenerateEAN13(DateTime.Now.Millisecond);
            }

            if (string.IsNullOrEmpty(product.ItemBarcode))
            {
                product.ItemBarcode = Core.Utilities.BarcodeUtility.GenerateEAN13(DateTime.Now.Millisecond + 1000);
            }

            return product;
        }

        /// <summary>
        /// Creates initial stock for imported product
        /// </summary>
        private async Task CreateInitialStockForImport(Product product, Dictionary<string, string> data, int userId)
        {
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
        }

        /// <summary>
        /// Creates CSV header row
        /// </summary>
        private string CreateCsvHeader()
        {
            return "Id,Name,Description,CategoryId,BoxBarcode,ItemBarcode,ItemsPerBox," +
                   "BoxPurchasePrice,BoxWholesalePrice,BoxSalePrice,ItemPurchasePrice,ItemWholesalePrice,ItemSalePrice," +
                   "SupplierId,SupplierInvoiceId,SupplierProductCode,TrackInventory,AllowNegativeInventory,Weight,Dimensions";
        }

        /// <summary>
        /// Creates CSV row for a product
        /// </summary>
        private string CreateCsvRow(Product product)
        {
            return $"{product.Id}," +
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
                   $"{product.SupplierInvoiceId}," +
                   $"\"{EscapeCsvField(product.SupplierProductCode)}\"," +
                   $"{product.TrackInventory}," +
                   $"{product.AllowNegativeInventory}," +
                   $"{product.Weight}," +
                   $"\"{EscapeCsvField(product.Dimensions)}\"";
        }

        // CSV helper methods
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

            return field.Replace("\"", "\"\"");
        }

        #endregion
    }
}