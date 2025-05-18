// OfflinePOS.Core/Models/DrawerOperation.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a cash drawer operation session
    /// </summary>
    public class DrawerOperation : EntityBase
    {
        /// <summary>
        /// User who opened/closed the drawer
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Starting balance at the beginning of the shift
        /// </summary>
        public decimal StartingBalance { get; set; }

        /// <summary>
        /// Ending balance at the end of the shift
        /// </summary>
        public decimal EndingBalance { get; set; }

        /// <summary>
        /// Expected balance based on transactions
        /// </summary>
        public decimal ExpectedBalance { get; set; }

        /// <summary>
        /// Difference between ending and expected balance
        /// </summary>
        public decimal Difference { get; set; }

        /// <summary>
        /// Status of the drawer (Open/Closed)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Status { get; set; }

        /// <summary>
        /// Time when the drawer was opened
        /// </summary>
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Time when the drawer was closed
        /// </summary>
        public DateTime? CloseTime { get; set; }

        /// <summary>
        /// Notes about the drawer operation
        /// </summary>
        [MaxLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<DrawerTransaction> DrawerTransactions { get; set; } = new List<DrawerTransaction>();
    }
}