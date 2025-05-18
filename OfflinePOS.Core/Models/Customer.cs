// OfflinePOS.Core/Models/Customer.cs
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a customer in the system
    /// </summary>
    public class Customer : EntityBase
    {
        /// <summary>
        /// Name of the customer
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Phone number of the customer
        /// </summary>
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Physical address of the customer
        /// </summary>
        [MaxLength(200)]
        public string Address { get; set; }

        /// <summary>
        /// Current balance amount owed by the customer
        /// </summary>
        public decimal CurrentBalance { get; set; }
    }
}