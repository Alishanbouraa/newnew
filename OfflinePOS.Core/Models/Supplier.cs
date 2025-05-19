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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Contact phone number
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Physical address
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Current balance owed to the supplier
        /// </summary>
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// Contact person's name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        /// <summary>
        /// Contact person's email
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Tax identification number
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TaxId { get; set; } = string.Empty;

        /// <summary>
        /// Payment terms (e.g., Net 30, COD)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PaymentTerms { get; set; } = string.Empty;
    }
}