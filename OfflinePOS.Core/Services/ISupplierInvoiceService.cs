// OfflinePOS.Core/Services/ISupplierInvoiceService.cs
using OfflinePOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing supplier invoices and payments
    /// </summary>
    public interface ISupplierInvoiceService
    {
        /// <summary>
        /// Gets all active supplier invoices
        /// </summary>
        Task<IEnumerable<SupplierInvoice>> GetAllInvoicesAsync();

        /// <summary>
        /// Gets invoices for a specific supplier
        /// </summary>
        Task<IEnumerable<SupplierInvoice>> GetInvoicesBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets invoices in a date range
        /// </summary>
        Task<IEnumerable<SupplierInvoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets an invoice by ID
        /// </summary>
        Task<SupplierInvoice> GetInvoiceByIdAsync(int id);

        /// <summary>
        /// Creates a new supplier invoice
        /// </summary>
        Task<SupplierInvoice> CreateInvoiceAsync(SupplierInvoice invoice);

        /// <summary>
        /// Updates an existing supplier invoice
        /// </summary>
        Task<bool> UpdateInvoiceAsync(SupplierInvoice invoice);

        /// <summary>
        /// Cancels a supplier invoice
        /// </summary>
        Task<bool> CancelInvoiceAsync(int invoiceId, string reason, int userId);

        /// <summary>
        /// Records a payment for an invoice
        /// </summary>
        Task<SupplierPayment> ProcessPaymentAsync(SupplierPayment payment);

        /// <summary>
        /// Gets payments for a supplier
        /// </summary>
        Task<IEnumerable<SupplierPayment>> GetPaymentsBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets payments for an invoice
        /// </summary>
        Task<IEnumerable<SupplierPayment>> GetPaymentsByInvoiceAsync(int invoiceId);

        /// <summary>
        /// Gets items for an invoice
        /// </summary>
        Task<IEnumerable<SupplierInvoiceItem>> GetInvoiceItemsAsync(int invoiceId);

        /// <summary>
        /// Gets unpaid invoices
        /// </summary>
        Task<IEnumerable<SupplierInvoice>> GetUnpaidInvoicesAsync();

        /// <summary>
        /// Gets overdue invoices
        /// </summary>
        Task<IEnumerable<SupplierInvoice>> GetOverdueInvoicesAsync();
    }
}