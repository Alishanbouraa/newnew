// OfflinePOS.Core/Models/StockAdjustment.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents an inventory adjustment transaction
    /// </summary>
    public class StockAdjustment : EntityBase
    {
        /// <summary>
        /// Product being adjusted
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Type of adjustment (Addition/Reduction/Transfer/Inventory)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string AdjustmentType { get; set; }

        /// <summary>
        /// Quantity of boxes adjusted
        /// </summary>
        public int BoxQuantity { get; set; }

        /// <summary>
        /// Quantity of items adjusted
        /// </summary>
        public int ItemQuantity { get; set; }

        /// <summary>
        /// Box quantity before adjustment
        /// </summary>
        public int PreviousBoxQuantity { get; set; }

        /// <summary>
        /// Item quantity before adjustment
        /// </summary>
        public int PreviousItemQuantity { get; set; }

        /// <summary>
        /// Box quantity after adjustment
        /// </summary>
        public int NewBoxQuantity { get; set; }

        /// <summary>
        /// Item quantity after adjustment
        /// </summary>
        public int NewItemQuantity { get; set; }

        /// <summary>
        /// Reference number (e.g., PO number, transfer ID)
        /// </summary>
        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        /// <summary>
        /// Reason for the adjustment
        /// </summary>
        [MaxLength(500)]
        public string Reason { get; set; }

        /// <summary>
        /// Date of the adjustment
        /// </summary>
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual Product Product { get; set; }
    }
}