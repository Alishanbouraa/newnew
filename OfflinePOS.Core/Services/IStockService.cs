// OfflinePOS.Core/Services/IStockService.cs
using OfflinePOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing inventory stock
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// Gets stock information for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Stock information</returns>
        Task<Stock> GetStockByProductIdAsync(int productId);

        /// <summary>
        /// Gets low stock products that need reordering
        /// </summary>
        /// <returns>List of low stock products with their stock info</returns>
        Task<IEnumerable<(Product Product, Stock Stock)>> GetLowStockProductsAsync();

        // File: OfflinePOS.Core/Services/IStockService.cs
        // Update the method signature to include the locationCode parameter

        /// <summary>
        /// Updates stock levels for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="boxQuantity">New box quantity</param>
        /// <param name="itemQuantity">New item quantity</param>
        /// <param name="adjustmentType">Type of adjustment</param>
        /// <param name="reason">Reason for adjustment</param>
        /// <param name="reference">Reference information</param>
        /// <param name="userId">User making the adjustment</param>
        /// <param name="locationCode">Optional location code</param>
        /// <returns>Updated stock</returns>
        Task<Stock> UpdateStockLevelsAsync(
            int productId,
            int boxQuantity,
            int itemQuantity,
            string adjustmentType,
            string reason,
            string reference,
            int userId,
            string locationCode = null);

        /// <summary>
        /// Converts boxes to items for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="boxesToConvert">Number of boxes to convert</param>
        /// <param name="userId">User performing the conversion</param>
        /// <returns>Updated stock</returns>
        Task<Stock> ConvertBoxesToItemsAsync(int productId, int boxesToConvert, int userId);

        /// <summary>
        /// Performs a full inventory count and updates stock levels
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="actualBoxCount">Actual box count from physical inventory</param>
        /// <param name="actualItemCount">Actual item count from physical inventory</param>
        /// <param name="notes">Inventory notes</param>
        /// <param name="userId">User performing the inventory</param>
        /// <returns>Updated stock</returns>
        Task<Stock> PerformInventoryCountAsync(
            int productId,
            int actualBoxCount,
            int actualItemCount,
            string notes,
            int userId);

        /// <summary>
        /// Gets adjustment history for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Stock adjustment history</returns>
        Task<IEnumerable<StockAdjustment>> GetStockAdjustmentHistoryAsync(
            int productId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Updates minimum stock levels for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="minimumBoxLevel">Minimum box level</param>
        /// <param name="minimumItemLevel">Minimum item level</param>
        /// <param name="reorderBoxQuantity">Reorder box quantity</param>
        /// <param name="userId">User making the update</param>
        /// <returns>Updated stock</returns>
        Task<Stock> UpdateMinimumStockLevelsAsync(
            int productId,
            int minimumBoxLevel,
            int minimumItemLevel,
            int reorderBoxQuantity,
            int userId);

        /// <summary>
        /// Updates the location code for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="locationCode">New location code</param>
        /// <param name="userId">User making the update</param>
        /// <returns>Updated stock</returns>
        Task<Stock> UpdateLocationCodeAsync(int productId, string locationCode, int userId);
    }
}