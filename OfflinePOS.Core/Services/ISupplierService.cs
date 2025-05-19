// OfflinePOS.Core/Services/ISupplierService.cs
using OfflinePOS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing suppliers/vendors
    /// </summary>
    public interface ISupplierService
    {
        /// <summary>
        /// Gets all active suppliers
        /// </summary>
        /// <returns>List of active suppliers</returns>
        Task<IEnumerable<Supplier>> GetAllSuppliersAsync();

        /// <summary>
        /// Gets a supplier by ID
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <returns>Supplier if found, null otherwise</returns>
        Task<Supplier> GetSupplierByIdAsync(int id);

        /// <summary>
        /// Creates a new supplier
        /// </summary>
        /// <param name="supplier">Supplier to create</param>
        /// <returns>Created supplier</returns>
        Task<Supplier> CreateSupplierAsync(Supplier supplier);

        /// <summary>
        /// Updates an existing supplier
        /// </summary>
        /// <param name="supplier">Supplier to update</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateSupplierAsync(Supplier supplier);

        /// <summary>
        /// Deletes a supplier by ID
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteSupplierAsync(int id);

        /// <summary>
        /// Searches for suppliers by name or contact info
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Matching suppliers</returns>
        Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm);

        /// <summary>
        /// Gets suppliers associated with a specific product
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Suppliers for the product</returns>
        Task<IEnumerable<Supplier>> GetSuppliersByProductAsync(int productId);
    }
}