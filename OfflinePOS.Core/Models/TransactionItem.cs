// OfflinePOS.Core/Models/TransactionItem.cs
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents an item in a transaction
    /// </summary>
    public class TransactionItem : EntityBase
    {
        /// <summary>
        /// Transaction to which this item belongs
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Product being sold or returned
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity of the product
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit type (Box/Item)
        /// </summary>
        [Required]
        [MaxLength(5)]
        public string UnitType { get; set; }

        /// <summary>
        /// Price per unit
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Discount percentage applied to this item
        /// </summary>
        public decimal DiscountPercentage { get; set; }

        /// <summary>
        /// Calculated discount amount
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Tax percentage applied to this item
        /// </summary>
        public decimal TaxPercentage { get; set; }

        /// <summary>
        /// Calculated tax amount
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Total amount for this item after discounts and taxes
        /// </summary>
        public decimal TotalAmount { get; set; }

        // Navigation properties
        public virtual Transaction Transaction { get; set; }
        public virtual Product Product { get; set; }
    }
}