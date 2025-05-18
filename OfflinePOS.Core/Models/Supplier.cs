// OfflinePOS.Core/Models/Supplier.cs
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a supplier or vendor of products
    /// </summary>
    public class Supplier : EntityBase
    {
        /// <summary>
        /// Name of the supplier
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Contact phone number
        /// </summary>
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Physical address
        /// </summary>
        [MaxLength(200)]
        public string Address { get; set; }

        /// <summary>
        /// Current balance owed to the supplier
        /// </summary>
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// Contact person's name
        /// </summary>
        [MaxLength(100)]
        public string ContactPerson { get; set; }

        /// <summary>
        /// Contact person's email
        /// </summary>
        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Tax identification number
        /// </summary>
        [MaxLength(50)]
        public string TaxId { get; set; }

        /// <summary>
        /// Payment terms (e.g., Net 30, COD)
        /// </summary>
        [MaxLength(50)]
        public string PaymentTerms { get; set; }
    }
}