// OfflinePOS.Core/Models/SupplierInvoice.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents an invoice from a supplier/vendor
    /// </summary>
    public class SupplierInvoice : EntityBase
    {
        /// <summary>
        /// Supplier who issued the invoice
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Invoice number from the supplier
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Total amount of the invoice
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Amount paid on this invoice
        /// </summary>
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// Remaining balance to be paid
        /// </summary>
        public decimal RemainingBalance { get; set; }

        /// <summary>
        /// Status of the invoice (Pending/Paid/PartiallyPaid/Cancelled)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

        /// <summary>
        /// Date of the invoice from the supplier
        /// </summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// Due date for payment
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Notes or comments about the invoice
        /// </summary>
        [MaxLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<SupplierInvoiceItem> Items { get; set; } = new List<SupplierInvoiceItem>();
        public virtual ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
    }
}