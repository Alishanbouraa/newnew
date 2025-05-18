// OfflinePOS.DataAccess/Services/StockService.cs
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
    /// Service for managing inventory stock
    /// </summary>
    public class StockService : IStockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StockService> _logger;

        /// <summary>
        /// Initializes a new instance of the StockService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public StockService(IUnitOfWork unitOfWork, ILogger<StockService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Stock> GetStockByProductIdAsync(int productId)
        {
            try
            {
                var stocks = await _unitOfWork.Stocks.GetAsync(s => s.ProductId == productId);
                var stock = stocks.FirstOrDefault();

                if (stock == null)
                {
                    // Create new stock record if it doesn't exist
                    var product = await _unitOfWork.Products.GetByIdAsync(productId);
                    if (product == null)
                        throw new InvalidOperationException($"Product with ID {productId} not found");

                    stock = new Stock
                    {
                        ProductId = productId,
                        BoxQuantity = 0,
                        ItemQuantity = 0,
                        MinimumBoxLevel = 1,
                        MinimumItemLevel = product.ItemsPerBox / 2,
                        ReorderBoxQuantity = 1,
                        StockStatus = "Out",
                        CreatedById = 1 // Default system user
                    };

                    await _unitOfWork.Stocks.AddAsync(stock);
                    await _unitOfWork.SaveChangesAsync();
                }

                return stock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<(Product Product, Stock Stock)>> GetLowStockProductsAsync()
        {
            try
            {
                // Get all active products with their stock information
                var products = await _unitOfWork.Products.GetAsync(p => p.IsActive && p.TrackInventory);
                var result = new List<(Product Product, Stock Stock)>();

                foreach (var product in products)
                {
                    var stock = await GetStockByProductIdAsync(product.Id);

                    // Check if stock is low or out
                    bool isLow = (stock.BoxQuantity <= stock.MinimumBoxLevel) &&
                                 (stock.ItemQuantity <= stock.MinimumItemLevel);

                    if (isLow)
                    {
                        // Calculate total equivalent items
                        int totalItems = (stock.BoxQuantity * product.ItemsPerBox) + stock.ItemQuantity;
                        int minimumItems = (stock.MinimumBoxLevel * product.ItemsPerBox) + stock.MinimumItemLevel;

                        // Update stock status
                        if (totalItems == 0)
                        {
                            stock.StockStatus = "Out";
                        }
                        else if (totalItems < minimumItems)
                        {
                            stock.StockStatus = "Low";
                        }

                        if (stock.StockStatus != "Available")
                        {
                            await _unitOfWork.Stocks.UpdateAsync(stock);
                            result.Add((product, stock));
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stock> UpdateStockLevelsAsync(
            int productId,
            int boxQuantityChange,
            int itemQuantityChange,
            string adjustmentType,
            string reason,
            string reference,
            int userId)
        {
            try
            {
                _unitOfWork.BeginTransaction();

                // Get current stock and product
                var stock = await GetStockByProductIdAsync(productId);
                var product = await _unitOfWork.Products.GetByIdAsync(productId);

                if (product == null)
                    throw new InvalidOperationException($"Product with ID {productId} not found");

                // Record previous quantities
                int previousBoxQuantity = stock.BoxQuantity;
                int previousItemQuantity = stock.ItemQuantity;

                // Calculate new quantities
                int newBoxQuantity = stock.BoxQuantity;
                int newItemQuantity = stock.ItemQuantity;

                // Apply adjustments based on type
                switch (adjustmentType.ToLower())
                {
                    case "addition":
                        newBoxQuantity += boxQuantityChange;
                        newItemQuantity += itemQuantityChange;
                        break;

                    case "reduction":
                        // Calculate total items
                        int currentTotalItems = (stock.BoxQuantity * product.ItemsPerBox) + stock.ItemQuantity;
                        int reductionTotalItems = (boxQuantityChange * product.ItemsPerBox) + itemQuantityChange;

                        // Check if sufficient stock exists
                        if (!product.AllowNegativeInventory && reductionTotalItems > currentTotalItems)
                        {
                            throw new InvalidOperationException("Insufficient stock for reduction");
                        }

                        newBoxQuantity -= boxQuantityChange;
                        newItemQuantity -= itemQuantityChange;

                        // Handle negative item quantity by converting boxes
                        while (newItemQuantity < 0 && newBoxQuantity > 0)
                        {
                            newBoxQuantity--;
                            newItemQuantity += product.ItemsPerBox;
                        }

                        // If still negative and we don't allow negative inventory, throw error
                        if (!product.AllowNegativeInventory && (newBoxQuantity < 0 || newItemQuantity < 0))
                        {
                            throw new InvalidOperationException("Insufficient stock for reduction");
                        }
                        break;

                    case "inventory":
                        // Direct update from inventory count
                        newBoxQuantity = boxQuantityChange;
                        newItemQuantity = itemQuantityChange;
                        break;

                    default:
                        throw new ArgumentException($"Invalid adjustment type: {adjustmentType}");
                }

                // Create stock adjustment record
                var adjustment = new StockAdjustment
                {
                    ProductId = productId,
                    AdjustmentType = adjustmentType,
                    BoxQuantity = boxQuantityChange,
                    ItemQuantity = itemQuantityChange,
                    PreviousBoxQuantity = previousBoxQuantity,
                    PreviousItemQuantity = previousItemQuantity,
                    NewBoxQuantity = newBoxQuantity,
                    NewItemQuantity = newItemQuantity,
                    ReferenceNumber = reference,
                    Reason = reason,
                    AdjustmentDate = DateTime.Now,
                    CreatedById = userId
                };

                await _unitOfWork.StockAdjustments.AddAsync(adjustment);

                // Update stock quantities
                stock.BoxQuantity = newBoxQuantity;
                stock.ItemQuantity = newItemQuantity;
                stock.LastUpdatedById = userId;
                stock.LastUpdatedDate = DateTime.Now;

                // Update stock status
                UpdateStockStatus(stock, product);

                await _unitOfWork.Stocks.UpdateAsync(stock);
                await _unitOfWork.SaveChangesAsync();

                _unitOfWork.CommitTransaction();
                return stock;
            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackTransaction();
                _logger.LogError(ex, "Error updating stock levels for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stock> ConvertBoxesToItemsAsync(int productId, int boxesToConvert, int userId)
        {
            try
            {
                // Get product and stock
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {productId} not found");

                var stock = await GetStockByProductIdAsync(productId);

                // Ensure enough boxes to convert
                if (boxesToConvert > stock.BoxQuantity)
                    throw new InvalidOperationException("Not enough boxes to convert");

                // Calculate item quantity to add
                int itemsToAdd = boxesToConvert * product.ItemsPerBox;

                // Create stock adjustment record
                string reason = $"Converted {boxesToConvert} boxes to {itemsToAdd} items";

                return await UpdateStockLevelsAsync(
                    productId,
                    -boxesToConvert,     // Reduce boxes
                    itemsToAdd,          // Increase items
                    "Conversion",
                    reason,
                    $"BOX2ITEM-{DateTime.Now:yyyyMMddHHmmss}",
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting boxes to items for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stock> PerformInventoryCountAsync(
            int productId,
            int actualBoxCount,
            int actualItemCount,
            string notes,
            int userId)
        {
            try
            {
                // Get product and stock
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {productId} not found");

                var stock = await GetStockByProductIdAsync(productId);

                // Calculate difference
                int boxDifference = actualBoxCount - stock.BoxQuantity;
                int itemDifference = actualItemCount - stock.ItemQuantity;

                // Create reference number
                string reference = $"INV-{DateTime.Now:yyyyMMdd}-{productId}";

                // Update stock
                await UpdateStockLevelsAsync(
                    productId,
                    actualBoxCount,      // Direct set to actual count
                    actualItemCount,     // Direct set to actual count
                    "Inventory",
                    notes ?? "Physical inventory count adjustment",
                    reference,
                    userId);

                // Update last inventory date
                stock.LastInventoryDate = DateTime.Now;
                await _unitOfWork.SaveChangesAsync();

                return stock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing inventory count for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StockAdjustment>> GetStockAdjustmentHistoryAsync(
            int productId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                // Ensure end date includes the full day
                endDate = endDate.Date.AddDays(1).AddTicks(-1);

                var adjustments = await _unitOfWork.StockAdjustments.GetAsync(
                    sa => sa.ProductId == productId &&
                          sa.AdjustmentDate >= startDate &&
                          sa.AdjustmentDate <= endDate);

                return adjustments.OrderByDescending(sa => sa.AdjustmentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock adjustment history for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stock> UpdateMinimumStockLevelsAsync(
            int productId,
            int minimumBoxLevel,
            int minimumItemLevel,
            int reorderBoxQuantity,
            int userId)
        {
            try
            {
                var stock = await GetStockByProductIdAsync(productId);
                var product = await _unitOfWork.Products.GetByIdAsync(productId);

                if (product == null)
                    throw new InvalidOperationException($"Product with ID {productId} not found");

                // Update reorder settings
                stock.MinimumBoxLevel = minimumBoxLevel;
                stock.MinimumItemLevel = minimumItemLevel;
                stock.ReorderBoxQuantity = reorderBoxQuantity;
                stock.LastUpdatedById = userId;
                stock.LastUpdatedDate = DateTime.Now;

                // Update status based on new minimum levels
                UpdateStockStatus(stock, product);

                await _unitOfWork.Stocks.UpdateAsync(stock);
                await _unitOfWork.SaveChangesAsync();

                return stock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating minimum stock levels for product {ProductId}", productId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stock> UpdateLocationCodeAsync(int productId, string locationCode, int userId)
        {
            try
            {
                var stock = await GetStockByProductIdAsync(productId);

                // Update location code
                stock.LocationCode = locationCode;
                stock.LastUpdatedById = userId;
                stock.LastUpdatedDate = DateTime.Now;

                await _unitOfWork.Stocks.UpdateAsync(stock);
                await _unitOfWork.SaveChangesAsync();

                return stock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location code for product {ProductId}", productId);
                throw;
            }
        }

        /// <summary>
        /// Updates the stock status based on current quantities and minimum levels
        /// </summary>
        /// <param name="stock">Stock to update</param>
        /// <param name="product">Product associated with the stock</param>
        private void UpdateStockStatus(Stock stock, Product product)
        {
            // Calculate total equivalent items
            int totalItems = (stock.BoxQuantity * product.ItemsPerBox) + stock.ItemQuantity;
            int minimumItems = (stock.MinimumBoxLevel * product.ItemsPerBox) + stock.MinimumItemLevel;

            if (totalItems <= 0)
            {
                stock.StockStatus = "Out";
            }
            else if (totalItems <= minimumItems)
            {
                stock.StockStatus = "Low";
            }
            else
            {
                stock.StockStatus = "Available";
            }
        }
    }
}