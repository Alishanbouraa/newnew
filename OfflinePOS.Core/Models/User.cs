using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a user/employee in the system
    /// </summary>
    public class User : EntityBase
    {
        /// <summary>
        /// Username for login
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        /// <summary>
        /// Hashed password
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Salt used in password hashing
        /// </summary>
        [Required]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Full name of the user
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        /// <summary>
        /// Role of the user (Admin, Cashier, etc.)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; }

        /// <summary>
        /// Job title for non-admin/cashier roles
        /// </summary>
        [MaxLength(50)]
        public string JobTitle { get; set; }

        /// <summary>
        /// Monthly salary amount
        /// </summary>
        public decimal MonthlySalary { get; set; }

        /// <summary>
        /// Working hours per day
        /// </summary>
        public int? WorkingHoursPerDay { get; set; }
    }
}