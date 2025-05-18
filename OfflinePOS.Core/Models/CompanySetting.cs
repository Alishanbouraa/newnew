using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents company configuration settings
    /// </summary>
    public class CompanySetting : EntityBase
    {
        /// <summary>
        /// Name of the company
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; }

        /// <summary>
        /// Company logo stored as binary data
        /// </summary>
        public byte[] Logo { get; set; }

        /// <summary>
        /// Physical address of the company
        /// </summary>
        [MaxLength(250)]
        public string Address { get; set; }

        /// <summary>
        /// Primary contact number
        /// </summary>
        [MaxLength(20)]
        public string PhoneNumber1 { get; set; }

        /// <summary>
        /// Secondary contact number
        /// </summary>
        [MaxLength(20)]
        public string PhoneNumber2 { get; set; }

        /// <summary>
        /// Current exchange rate for USD to local currency
        /// </summary>
        public decimal DollarRate { get; set; }

        /// <summary>
        /// Main currency used in the system (LBP, USD, etc.)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string MainCurrency { get; set; }

        /// <summary>
        /// Default language for the application
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string DefaultLanguage { get; set; } = "en-US";
    }
}