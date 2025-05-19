// OfflinePOS.Core/Models/Product.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a product in the inventory
    /// </summary>
    public class Product : EntityBase
    {
        /// <summary>
        /// Category to which the product belongs
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Name of the product
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Description of the product
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Barcode for identifying boxes of the product
        /// </summary>
        [MaxLength(30)]
        public string BoxBarcode { get; set; }

        /// <summary>
        /// Barcode for identifying individual items
        /// </summary>
        [MaxLength(30)]
        public string ItemBarcode { get; set; }

        /// <summary>
        /// Number of individual items in a box
        /// </summary>
        public int ItemsPerBox { get; set; } = 1;

        /// <summary>
        /// Purchase price for a box
        /// </summary>
        public decimal BoxPurchasePrice { get; set; }

        /// <summary>
        /// Wholesale price for a box
        /// </summary>
        public decimal BoxWholesalePrice { get; set; }

        /// <summary>
        /// Sale price for a box
        /// </summary>
        public decimal BoxSalePrice { get; set; }

        /// <summary>
        /// Purchase price for an individual item
        /// </summary>
        public decimal ItemPurchasePrice { get; set; }

        /// <summary>
        /// Wholesale price for an individual item
        /// </summary>
        public decimal ItemWholesalePrice { get; set; }

        /// <summary>
        /// Sale price for an individual item
        /// </summary>
        public decimal ItemSalePrice { get; set; }
        /// <summary>
        /// Vendor/supplier ID for the product
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// Vendor/supplier product code
        /// </summary>
        [MaxLength(50)]
        public string SupplierProductCode { get; set; }

        /// <summary>
        /// Manufacturer's suggested retail price
        /// </summary>
        public decimal MSRP { get; set; }

        /// <summary>
        /// Flag indicating if the product tracks inventory
        /// </summary>
        public bool TrackInventory { get; set; } = true;

        /// <summary>
        /// Flag indicating if the product can be sold when out of stock
        /// </summary>
        public bool AllowNegativeInventory { get; set; } = false;

        /// <summary>
        /// Weight of the item in kg
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Dimensions in format LxWxH (cm)
        /// </summary>
        [MaxLength(50)]
        public string Dimensions { get; set; }


        // Navigation properties
        public virtual Stock Stock { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();
        public virtual Category Category { get; set; }
    }
}