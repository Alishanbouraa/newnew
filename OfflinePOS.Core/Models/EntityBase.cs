using System;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Base class for all domain entities providing common properties and functionality
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Flag indicating if the entity is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date and time when the entity was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// User who created the entity
        /// </summary>
        public int CreatedById { get; set; }

        /// <summary>
        /// Date and time when the entity was last updated
        /// </summary>
        public DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// User who last updated the entity
        /// </summary>
        public int? LastUpdatedById { get; set; }

        /// <summary>
        /// Concurrency token to detect conflicting updates
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}