// OfflinePOS.Core/Models/Product.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OfflinePOS.Core.Models
{
    /// <summary>
    /// Represents a product in the inventory with comprehensive catalog availability control
    /// Supports dual-mode operation: Inventory Management and Product Catalog
    /// </summary>
    public class Product : EntityBase
    {
        #region Product Identity

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
        /// Description of the product (optional)
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        #endregion

        #region Barcode Management

        /// <summary>
        /// Barcode for identifying boxes of the product
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string BoxBarcode { get; set; }

        /// <summary>
        /// Barcode for identifying individual items
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string ItemBarcode { get; set; }

        /// <summary>
        /// Number of individual items in a box
        /// </summary>
        public int ItemsPerBox { get; set; } = 1;

        #endregion

        #region Pricing Structure

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
        /// Manufacturer's suggested retail price
        /// </summary>
        public decimal MSRP { get; set; }

        #endregion

        #region Supplier Information

        /// <summary>
        /// Vendor/supplier ID for the product
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// Reference to a supplier invoice (if this product was from an invoice)
        /// </summary>
        public int? SupplierInvoiceId { get; set; }

        /// <summary>
        /// Vendor/supplier product code
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string SupplierProductCode { get; set; }

        #endregion

        #region Inventory Control Settings

        /// <summary>
        /// Flag indicating if the product tracks inventory
        /// </summary>
        public bool TrackInventory { get; set; } = true;

        /// <summary>
        /// Flag indicating if the product can be sold when out of stock
        /// </summary>
        public bool AllowNegativeInventory { get; set; } = false;

        #endregion

        #region ENHANCED: Inventory vs Catalog Management

        /// <summary>
        /// Flag indicating if the product is available for sale in the catalog
        /// FALSE = Product is in Inventory Management (main stock storage)
        /// TRUE = Product is in Product Catalog (available for customer purchase)
        /// </summary>
        public bool IsAvailableForSale { get; set; } = false;

        /// <summary>
        /// Date when the product was made available for sale
        /// NULL = Never been made available for sale (inventory only)
        /// </summary>
        public DateTime? AvailableForSaleDate { get; set; }

        /// <summary>
        /// User who made the product available for sale
        /// References the user who performed the transfer from inventory to catalog
        /// </summary>
        public int? AvailableForSaleById { get; set; }

        #endregion

        #region Physical Attributes

        /// <summary>
        /// Weight of the item in kg
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Dimensions in format LxWxH (cm) (optional)
        /// </summary>
        [MaxLength(50)]
        public string? Dimensions { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Associated stock information for inventory tracking
        /// </summary>
        public virtual Stock Stock { get; set; }

        /// <summary>
        /// Supplier/vendor who provides this product
        /// </summary>
        public virtual Supplier Supplier { get; set; }

        /// <summary>
        /// Supplier invoice associated with this product (if applicable)
        /// </summary>
        public virtual SupplierInvoice SupplierInvoice { get; set; }

        /// <summary>
        /// User who made the product available for sale
        /// </summary>
        public virtual User AvailableForSaleBy { get; set; }

        /// <summary>
        /// Collection of stock adjustment transactions
        /// </summary>
        public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

        /// <summary>
        /// Product category for organization and filtering
        /// </summary>
        public virtual Category Category { get; set; }

        #endregion

        #region Business Logic Helpers

        /// <summary>
        /// Determines if this product is currently in inventory management
        /// </summary>
        public bool IsInInventory => !IsAvailableForSale;

        /// <summary>
        /// Determines if this product is currently in the sales catalog
        /// </summary>
        public bool IsInCatalog => IsAvailableForSale;

        /// <summary>
        /// Gets the display status for UI purposes
        /// </summary>
        public string AvailabilityStatus => IsAvailableForSale ? "Available for Sale" : "In Inventory";

        /// <summary>
        /// Gets the days since product was made available for sale
        /// </summary>
        public int? DaysInCatalog => AvailableForSaleDate.HasValue
            ? (int)(DateTime.Now - AvailableForSaleDate.Value).TotalDays
            : null;

        #endregion
    }
}