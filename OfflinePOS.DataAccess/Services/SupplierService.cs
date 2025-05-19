// OfflinePOS.DataAccess/Services/SupplierService.cs
using Microsoft.EntityFrameworkCore;
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
    /// Service implementation for managing suppliers/vendors
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

                // Explicitly create a new entity to avoid tracking issues
                var newSupplier = new Supplier
                {
                    Name = supplier.Name,
                    ContactPerson = supplier.ContactPerson,
                    PhoneNumber = supplier.PhoneNumber,
                    Email = supplier.Email,
                    Address = supplier.Address,
                    TaxId = supplier.TaxId,
                    PaymentTerms = supplier.PaymentTerms,
                    CurrentBalance = supplier.CurrentBalance,
                    CreatedById = supplier.CreatedById,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                await _unitOfWork.Suppliers.AddAsync(newSupplier);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Supplier created: {SupplierName}", newSupplier.Name);
                return newSupplier;
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
                // Get the entity from the database first
                var existingSupplier = await _unitOfWork.Suppliers.GetByIdAsync(supplier.Id);
                if (existingSupplier == null)
                    return false;

                // Check if name is unique
                var duplicateNameCheck = await _unitOfWork.Suppliers.GetAsync(
                    s => s.Name == supplier.Name && s.Id != supplier.Id && s.IsActive);

                if (duplicateNameCheck.Any())
                    throw new InvalidOperationException($"Another supplier with name '{supplier.Name}' already exists");

                // Update properties directly on the tracked entity
                existingSupplier.Name = supplier.Name;
                existingSupplier.ContactPerson = supplier.ContactPerson;
                existingSupplier.PhoneNumber = supplier.PhoneNumber;
                existingSupplier.Email = supplier.Email;
                existingSupplier.Address = supplier.Address;
                existingSupplier.TaxId = supplier.TaxId;
                existingSupplier.PaymentTerms = supplier.PaymentTerms;
                existingSupplier.LastUpdatedById = supplier.LastUpdatedById;
                existingSupplier.LastUpdatedDate = DateTime.Now;

                // No need to call Update() since the entity is already tracked
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Supplier updated: {SupplierName}", supplier.Name);
                return true;
            }
            catch (Exception ex)
            {
                // Log all exceptions
                _logger.LogError(ex, "Error updating supplier {SupplierId}", supplier.Id);

                // Check for concurrency exception specifically
                if (ex is DbUpdateConcurrencyException)
                {
                    // Rethrow as a more specific exception for the UI to handle
                    throw new InvalidOperationException("The supplier was modified by another user. Please refresh and try again.", ex);
                }

                // Rethrow the original exception
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

                // Check if supplier has any invoices
                var hasInvoices = await _unitOfWork.SupplierInvoices.ExistsAsync(i => i.SupplierId == id && i.IsActive);
                if (hasInvoices)
                    throw new InvalidOperationException("Cannot delete supplier that has invoices");

                // Soft delete - set IsActive to false
                supplier.IsActive = false;
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