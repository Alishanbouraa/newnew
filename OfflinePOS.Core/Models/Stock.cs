// OfflinePOS.Core/Models/Stock.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents inventory stock information for a product
    /// </summary>
    public class Stock : EntityBase
    {
        /// <summary>
        /// Product associated with this stock
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity of boxes in stock
        /// </summary>
        public int BoxQuantity { get; set; }

        /// <summary>
        /// Quantity of individual items in stock
        /// </summary>
        public int ItemQuantity { get; set; }

        /// <summary>
        /// Minimum box quantity before reordering
        /// </summary>
        public int MinimumBoxLevel { get; set; }

        /// <summary>
        /// Minimum item quantity before reordering
        /// </summary>
        public int MinimumItemLevel { get; set; }

        /// <summary>
        /// Reorder quantity for boxes when minimum level is reached
        /// </summary>
        public int ReorderBoxQuantity { get; set; }

        /// <summary>
        /// Location code in warehouse or shelf
        /// </summary>
        [MaxLength(50)]
        public string LocationCode { get; set; }

        /// <summary>
        /// Last physical inventory date
        /// </summary>
        public DateTime? LastInventoryDate { get; set; }

        /// <summary>
        /// Status of the stock (Available/Low/Out)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string StockStatus { get; set; } = "Available";

        // Navigation property
        public virtual Product Product { get; set; }
    }
}