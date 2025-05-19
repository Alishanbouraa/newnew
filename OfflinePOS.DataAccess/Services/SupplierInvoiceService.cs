// OfflinePOS.DataAccess/Services/SupplierInvoiceService.cs
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
    /// Service for managing supplier invoices and payments
    /// </summary>
    public class SupplierInvoiceService : ISupplierInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierInvoiceService> _logger;

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceService class
        /// </summary>
        public SupplierInvoiceService(IUnitOfWork unitOfWork, ILogger<SupplierInvoiceService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierInvoice>> GetAllInvoicesAsync()
        {
            try
            {
                var invoices = await _unitOfWork.SupplierInvoices.GetAsync(i => i.IsActive);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all supplier invoices");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierInvoice>> GetInvoicesBySupplierAsync(int supplierId)
        {
            try
            {
                var invoices = await _unitOfWork.SupplierInvoices.GetAsync(
                    i => i.SupplierId == supplierId && i.IsActive);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierInvoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Adjust end date to include the entire day
                endDate = endDate.Date.AddDays(1).AddTicks(-1);

                var invoices = await _unitOfWork.SupplierInvoices.GetAsync(
                    i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate && i.IsActive);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for date range");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SupplierInvoice> GetInvoiceByIdAsync(int id)
        {
            try
            {
                var invoice = await _unitOfWork.SupplierInvoices.GetByIdAsync(id);
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SupplierInvoice> CreateInvoiceAsync(SupplierInvoice invoice)
        {
            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Validate supplier exists
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(invoice.SupplierId);
                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {invoice.SupplierId} not found");

                // Calculate totals if not already set
                if (invoice.TotalAmount == 0 && invoice.Items != null && invoice.Items.Any())
                {
                    invoice.TotalAmount = invoice.Items.Sum(i => i.TotalAmount);
                }

                // Set remaining balance
                invoice.RemainingBalance = invoice.TotalAmount - invoice.PaidAmount;

                // Set status based on payment
                if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = "Paid";
                    invoice.RemainingBalance = 0;
                }
                else if (invoice.PaidAmount > 0)
                {
                    invoice.Status = "PartiallyPaid";
                }
                else
                {
                    invoice.Status = "Pending";
                }

                // Add invoice
                await _unitOfWork.SupplierInvoices.AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                // Add invoice items if provided
                if (invoice.Items != null && invoice.Items.Any())
                {
                    foreach (var item in invoice.Items)
                    {
                        item.InvoiceId = invoice.Id;
                        await _unitOfWork.SupplierInvoiceItems.AddAsync(item);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                // Update supplier balance
                supplier.CurrentBalance += invoice.RemainingBalance;
                await _unitOfWork.Suppliers.UpdateAsync(supplier);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Created supplier invoice {InvoiceNumber} for supplier {SupplierId}",
                    invoice.InvoiceNumber, invoice.SupplierId);

                return invoice;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating supplier invoice");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateInvoiceAsync(SupplierInvoice invoice)
        {
            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Get existing invoice
                var existingInvoice = await _unitOfWork.SupplierInvoices.GetByIdAsync(invoice.Id);
                if (existingInvoice == null)
                    return false;

                // Calculate the difference in remaining balance
                decimal oldRemainingBalance = existingInvoice.RemainingBalance;

                // Update invoice properties
                existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
                existingInvoice.InvoiceDate = invoice.InvoiceDate;
                existingInvoice.DueDate = invoice.DueDate;
                existingInvoice.Notes = invoice.Notes;
                existingInvoice.LastUpdatedById = invoice.LastUpdatedById;
                existingInvoice.LastUpdatedDate = DateTime.Now;

                // Recalculate totals from items if items are provided
                if (invoice.Items != null && invoice.Items.Any())
                {
                    existingInvoice.TotalAmount = invoice.Items.Sum(i => i.TotalAmount);
                }
                else
                {
                    // Get existing items
                    var existingItems = await _unitOfWork.SupplierInvoiceItems.GetAsync(
                        i => i.InvoiceId == invoice.Id);
                    existingInvoice.TotalAmount = existingItems.Sum(i => i.TotalAmount);
                }

                // Recalculate remaining balance
                existingInvoice.RemainingBalance = existingInvoice.TotalAmount - existingInvoice.PaidAmount;

                // Update status
                if (existingInvoice.RemainingBalance <= 0)
                {
                    existingInvoice.Status = "Paid";
                    existingInvoice.RemainingBalance = 0;
                }
                else if (existingInvoice.PaidAmount > 0)
                {
                    existingInvoice.Status = "PartiallyPaid";
                }
                else
                {
                    existingInvoice.Status = "Pending";
                }

                await _unitOfWork.SupplierInvoices.UpdateAsync(existingInvoice);
                await _unitOfWork.SaveChangesAsync();

                // Update supplier balance if the remaining balance changed
                if (existingInvoice.RemainingBalance != oldRemainingBalance)
                {
                    var supplier = await _unitOfWork.Suppliers.GetByIdAsync(existingInvoice.SupplierId);
                    if (supplier != null)
                    {
                        supplier.CurrentBalance += (existingInvoice.RemainingBalance - oldRemainingBalance);
                        await _unitOfWork.Suppliers.UpdateAsync(supplier);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Updated supplier invoice {InvoiceId}", invoice.Id);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating supplier invoice {InvoiceId}", invoice.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CancelInvoiceAsync(int invoiceId, string reason, int userId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Get existing invoice
                var invoice = await _unitOfWork.SupplierInvoices.GetByIdAsync(invoiceId);
                if (invoice == null)
                    return false;

                // Check if already cancelled
                if (invoice.Status == "Cancelled")
                    return true;

                // Store the remaining balance to adjust supplier
                decimal remainingBalance = invoice.RemainingBalance;

                // Update invoice
                invoice.Status = "Cancelled";
                invoice.Notes = string.IsNullOrEmpty(invoice.Notes)
                    ? $"Cancelled: {reason}"
                    : $"{invoice.Notes}\nCancelled: {reason}";
                invoice.LastUpdatedById = userId;
                invoice.LastUpdatedDate = DateTime.Now;

                await _unitOfWork.SupplierInvoices.UpdateAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                // Update supplier balance
                if (remainingBalance > 0)
                {
                    var supplier = await _unitOfWork.Suppliers.GetByIdAsync(invoice.SupplierId);
                    if (supplier != null)
                    {
                        supplier.CurrentBalance -= remainingBalance;
                        await _unitOfWork.Suppliers.UpdateAsync(supplier);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Cancelled supplier invoice {InvoiceId}", invoiceId);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error cancelling supplier invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SupplierPayment> ProcessPaymentAsync(SupplierPayment payment)
        {
            if (payment == null)
                throw new ArgumentNullException(nameof(payment));

            if (payment.Amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero", nameof(payment));

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Validate supplier
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(payment.SupplierId);
                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {payment.SupplierId} not found");

                // Add the payment
                await _unitOfWork.SupplierPayments.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // If this is for a specific invoice, update the invoice
                if (payment.InvoiceId.HasValue)
                {
                    var invoice = await _unitOfWork.SupplierInvoices.GetByIdAsync(payment.InvoiceId.Value);
                    if (invoice != null)
                    {
                        invoice.PaidAmount += payment.Amount;
                        invoice.RemainingBalance = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);

                        // Update status
                        if (invoice.RemainingBalance <= 0)
                        {
                            invoice.Status = "Paid";
                        }
                        else if (invoice.PaidAmount > 0)
                        {
                            invoice.Status = "PartiallyPaid";
                        }

                        await _unitOfWork.SupplierInvoices.UpdateAsync(invoice);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                // Update supplier balance
                supplier.CurrentBalance = Math.Max(0, supplier.CurrentBalance - payment.Amount);
                await _unitOfWork.Suppliers.UpdateAsync(supplier);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Processed supplier payment of {Amount} for supplier {SupplierId}",
                    payment.Amount, payment.SupplierId);

                return payment;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing supplier payment");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierPayment>> GetPaymentsBySupplierAsync(int supplierId)
        {
            try
            {
                var payments = await _unitOfWork.SupplierPayments.GetAsync(
                    p => p.SupplierId == supplierId && p.IsActive);
                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierPayment>> GetPaymentsByInvoiceAsync(int invoiceId)
        {
            try
            {
                var payments = await _unitOfWork.SupplierPayments.GetAsync(
                    p => p.InvoiceId == invoiceId && p.IsActive);
                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierInvoiceItem>> GetInvoiceItemsAsync(int invoiceId)
        {
            try
            {
                var items = await _unitOfWork.SupplierInvoiceItems.GetAsync(
                    i => i.InvoiceId == invoiceId);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierInvoice>> GetUnpaidInvoicesAsync()
        {
            try
            {
                var invoices = await _unitOfWork.SupplierInvoices.GetAsync(
                    i => (i.Status == "Pending" || i.Status == "PartiallyPaid") && i.IsActive);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unpaid invoices");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SupplierInvoice>> GetOverdueInvoicesAsync()
        {
            try
            {
                var today = DateTime.Today;
                var invoices = await _unitOfWork.SupplierInvoices.GetAsync(
                    i => (i.Status == "Pending" || i.Status == "PartiallyPaid") &&
                         i.DueDate.HasValue && i.DueDate.Value < today &&
                         i.IsActive);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue invoices");
                throw;
            }
        }
    }
}