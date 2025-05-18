// OfflinePOS.Core/Models/Category.cs
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a category for products or expenses
    /// </summary>
    public class Category : EntityBase
    {
        /// <summary>
        /// Name of the category
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Type of category (Product/Expense)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Type { get; set; }

        /// <summary>
        /// Description of the category
        /// </summary>
        [MaxLength(200)]
        public string Description { get; set; }
    }
}