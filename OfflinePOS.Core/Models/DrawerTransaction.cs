// OfflinePOS.Core/Models/DrawerTransaction.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a cash in/out transaction for a drawer
    /// </summary>
    public class DrawerTransaction : EntityBase
    {
        /// <summary>
        /// Drawer operation associated with this transaction
        /// </summary>
        public int DrawerOperationId { get; set; }

        /// <summary>
        /// Amount of the transaction
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Type of transaction (CashIn/CashOut)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Type { get; set; }

        /// <summary>
        /// Reason for the transaction
        /// </summary>
        [MaxLength(200)]
        public string Reason { get; set; }

        /// <summary>
        /// Reference information for the transaction
        /// </summary>
        [MaxLength(50)]
        public string Reference { get; set; }

        /// <summary>
        /// Time of the transaction
        /// </summary>
        public DateTime TransactionTime { get; set; } = DateTime.Now;

        /// <summary>
        /// User who recorded the transaction
        /// </summary>
        public int RecordedById { get; set; }

        // Navigation properties
        public virtual DrawerOperation DrawerOperation { get; set; }
        public virtual User RecordedBy { get; set; }
    }
}