// OfflinePOS.Core/Models/SupplierPayment.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a payment made to a supplier
    /// </summary>
    public class SupplierPayment : EntityBase
    {
        /// <summary>
        /// Supplier receiving the payment
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Invoice being paid, if applicable
        /// </summary>
        public int? InvoiceId { get; set; }

        /// <summary>
        /// Amount of the payment
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Method of payment (Cash/Card/Check/Transfer)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; }

        /// <summary>
        /// Reference information (check number, transaction ID, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Reference { get; set; }

        /// <summary>
        /// Notes about the payment
        /// </summary>
        [MaxLength(200)]
        public string Notes { get; set; }

        /// <summary>
        /// Date of the payment
        /// </summary>
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        /// <summary>
        /// User who processed the payment
        /// </summary>
        public int ProcessedById { get; set; }

        // Navigation properties
        public virtual Supplier Supplier { get; set; }
        public virtual SupplierInvoice Invoice { get; set; }
        public virtual User ProcessedBy { get; set; }
    }
}