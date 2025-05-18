// OfflinePOS.Core/Models/Transaction.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a sales or return transaction
    /// </summary>
    public class Transaction : EntityBase
    {
        /// <summary>
        /// User who processed the transaction
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Customer associated with the transaction, if any
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Invoice number for the transaction
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Subtotal amount before discounts and taxes
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Discount percentage applied to the transaction
        /// </summary>
        public decimal DiscountPercentage { get; set; }

        /// <summary>
        /// Calculated discount amount
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Tax percentage applied to the transaction
        /// </summary>
        public decimal TaxPercentage { get; set; }

        /// <summary>
        /// Calculated tax amount
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Total amount after discounts and taxes
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Amount paid by the customer
        /// </summary>
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// Remaining balance to be paid
        /// </summary>
        public decimal RemainingBalance { get; set; }

        /// <summary>
        /// Method of payment (Cash, Card, etc.)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; }

        /// <summary>
        /// Type of transaction (Sale/Return)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string TransactionType { get; set; }

        /// <summary>
        /// Status of the transaction (Completed/Pending/Cancelled)
        /// </summary>
        [Required]
        [MaxLength(15)]
        public string Status { get; set; }

        /// <summary>
        /// Date and time when the transaction occurred
        /// </summary>
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Drawer operation ID associated with this transaction
        /// </summary>
        public int DrawerOperationId { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual DrawerOperation DrawerOperation { get; set; }
        public virtual ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }
}