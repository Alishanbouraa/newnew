// OfflinePOS.Core/Models/SupplierInvoiceItem.cs
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents an item line in a supplier invoice
    /// </summary>
    public class SupplierInvoiceItem : EntityBase
    {
        /// <summary>
        /// Invoice this item belongs to
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Product being purchased
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity of boxes purchased
        /// </summary>
        public int BoxQuantity { get; set; }

        /// <summary>
        /// Quantity of individual items (not in boxes)
        /// </summary>
        public int ItemQuantity { get; set; }

        /// <summary>
        /// Purchase price per box
        /// </summary>
        public decimal BoxPurchasePrice { get; set; }

        /// <summary>
        /// Purchase price per individual item
        /// </summary>
        public decimal ItemPurchasePrice { get; set; }

        /// <summary>
        /// Total amount for this line item
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Description or notes for this item
        /// </summary>
        [MaxLength(200)]
        public string Description { get; set; }

        // Navigation properties
        public virtual SupplierInvoice Invoice { get; set; }
        public virtual Product Product { get; set; }
    }
}