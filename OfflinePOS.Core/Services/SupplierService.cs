using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Services
{
    /// <summary>
    /// Service for managing suppliers/vendors
    /// </summary>
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierService> _logger;

        /// <summary>
        /// Initializes a new instance of the SupplierService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public SupplierService(IUnitOfWork unitOfWork, ILogger<SupplierService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync()
        {
            try
            {
                return await _unitOfWork.Suppliers.GetAsync(s => s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all suppliers");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Supplier> GetSupplierByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Suppliers.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier by ID {SupplierId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            try
            {
                // Validate supplier name is unique
                var existing = await _unitOfWork.Suppliers.GetAsync(
                    s => s.Name == supplier.Name && s.IsActive);

                if (existing.Any())
                    throw new InvalidOperationException($"A supplier with name '{supplier.Name}' already exists");

                await _unitOfWork.Suppliers.AddAsync(supplier);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Supplier created: {SupplierName}", supplier.Name);
                return supplier;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier {SupplierName}", supplier.Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            try
            {
                // Check if supplier exists
                var existingSupplier = await _unitOfWork.Suppliers.GetByIdAsync(supplier.Id);
                if (existingSupplier == null)
                    return false;

                // Check if name is unique
                var duplicateNameCheck = await _unitOfWork.Suppliers.GetAsync(
                    s => s.Name == supplier.Name && s.Id != supplier.Id && s.IsActive);

                if (duplicateNameCheck.Any())
                    throw new InvalidOperationException($"Another supplier with name '{supplier.Name}' already exists");

                await _unitOfWork.Suppliers.UpdateAsync(supplier);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Supplier updated: {SupplierName}", supplier.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", supplier.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteSupplierAsync(int id)
        {
            try
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
                if (supplier == null)
                    return false;

                // Check if supplier is used by any products
                var hasProducts = await _unitOfWork.Products.ExistsAsync(p => p.SupplierId == id && p.IsActive);
                if (hasProducts)
                    throw new InvalidOperationException("Cannot delete supplier that is used by products");

                // Soft delete - set IsActive to false
                supplier.IsActive = false;
                await _unitOfWork.Suppliers.UpdateAsync(supplier);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Supplier soft deleted: {SupplierId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Enumerable.Empty<Supplier>();

            try
            {
                return await _unitOfWork.Suppliers.GetAsync(
                    s => (s.Name.Contains(searchTerm) ||
                         (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)) ||
                         (s.PhoneNumber != null && s.PhoneNumber.Contains(searchTerm)) ||
                         (s.Email != null && s.Email.Contains(searchTerm))) &&
                         s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Supplier>> GetSuppliersByProductAsync(int productId)
        {
            try
            {
                // Get the product to find its supplier ID
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null || !product.SupplierId.HasValue)
                    return Enumerable.Empty<Supplier>();

                // Get the supplier
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(product.SupplierId.Value);
                if (supplier == null)
                    return Enumerable.Empty<Supplier>();

                return new List<Supplier> { supplier };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suppliers for product {ProductId}", productId);
                throw;
            }
        }
    
    }
}