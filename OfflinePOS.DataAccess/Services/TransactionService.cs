// OfflinePOS.DataAccess/Services/TransactionService.cs
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
    /// Service for managing sales transactions
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionService> _logger;

        /// <summary>
        /// Initializes a new instance of the TransactionService class
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        /// <param name="logger">Logger</param>
        public TransactionService(IUnitOfWork unitOfWork, ILogger<TransactionService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            try
            {
                _unitOfWork.BeginTransaction();

                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(transaction.InvoiceNumber))
                {
                    transaction.InvoiceNumber = await GenerateInvoiceNumberAsync();
                }

                // Set transaction date if not provided
                if (transaction.TransactionDate == default)
                {
                    transaction.TransactionDate = DateTime.Now;
                }

                // Calculate totals if not already calculated
                if (transaction.Total == 0)
                {
                    CalculateTransactionTotals(transaction);
                }

                // Add transaction
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // If customer is provided, update customer balance
                if (transaction.CustomerId.HasValue && transaction.RemainingBalance > 0)
                {
                    var customer = await _unitOfWork.Customers.GetByIdAsync(transaction.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.CurrentBalance += transaction.RemainingBalance;
                        await _unitOfWork.Customers.UpdateAsync(customer);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                _unitOfWork.CommitTransaction();
                _logger.LogInformation("Transaction created: {InvoiceNumber}", transaction.InvoiceNumber);
                return transaction;
            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackTransaction();
                _logger.LogError(ex, "Error creating transaction");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Transactions.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction by ID {TransactionId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Transaction> GetTransactionByInvoiceNumberAsync(string invoiceNumber)
        {
            if (string.IsNullOrEmpty(invoiceNumber))
                return null;

            try
            {
                var transactions = await _unitOfWork.Transactions.GetAsync(t => t.InvoiceNumber == invoiceNumber);
                return transactions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction by invoice number {InvoiceNumber}", invoiceNumber);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Adjust end date to include the full day
                var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

                return await _unitOfWork.Transactions.GetAsync(
                    t => t.TransactionDate >= startDate && t.TransactionDate <= adjustedEndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions by date range");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Transaction>> GetTransactionsByCustomerAsync(int customerId)
        {
            try
            {
                return await _unitOfWork.Transactions.GetAsync(t => t.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions by customer {CustomerId}", customerId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Transaction>> GetTransactionsByDrawerOperationAsync(int drawerOperationId)
        {
            try
            {
                return await _unitOfWork.Transactions.GetAsync(t => t.DrawerOperationId == drawerOperationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions by drawer operation {DrawerOperationId}", drawerOperationId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, string status)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null)
                    return false;

                transaction.Status = status;
                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Transaction status updated: {TransactionId}, Status: {Status}", transactionId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Transaction> ProcessPaymentAsync(int transactionId, decimal amount, string paymentMethod)
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null)
                    throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

                // Update paid amount and remaining balance
                transaction.PaidAmount += amount;
                transaction.RemainingBalance = Math.Max(0, transaction.Total - transaction.PaidAmount);
                transaction.PaymentMethod = paymentMethod;

                // Update status if fully paid
                if (transaction.RemainingBalance == 0)
                {
                    transaction.Status = "Completed";
                }

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // Update customer balance if applicable
                if (transaction.CustomerId.HasValue)
                {
                    var customer = await _unitOfWork.Customers.GetByIdAsync(transaction.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.CurrentBalance = Math.Max(0, customer.CurrentBalance - amount);
                        await _unitOfWork.Customers.UpdateAsync(customer);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                _unitOfWork.CommitTransaction();
                _logger.LogInformation("Payment processed for transaction {TransactionId}: {Amount}", transactionId, amount);
                return transaction;
            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackTransaction();
                _logger.LogError(ex, "Error processing payment");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateInvoiceNumberAsync()
        {
            try
            {
                // Format: INV-YYYYMMDD-XXXX
                string dateComponent = DateTime.Now.ToString("yyyyMMdd");

                // Get the highest invoice number for today
                var transactions = await _unitOfWork.Transactions.GetAsync(
                    t => t.InvoiceNumber.StartsWith($"INV-{dateComponent}"));

                int sequenceNumber = 1;

                if (transactions.Any())
                {
                    var maxInvoice = transactions
                        .Select(t => t.InvoiceNumber)
                        .OrderByDescending(i => i)
                        .FirstOrDefault();

                    if (maxInvoice != null)
                    {
                        var parts = maxInvoice.Split('-');
                        if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                        {
                            sequenceNumber = lastSequence + 1;
                        }
                    }
                }

                return $"INV-{dateComponent}-{sequenceNumber:D4}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                throw;
            }
        }

        /// <summary>
        /// Calculates totals for a transaction
        /// </summary>
        /// <param name="transaction">Transaction to calculate</param>
        private void CalculateTransactionTotals(Transaction transaction)
        {
            if (transaction.Items == null || !transaction.Items.Any())
                return;

            // Calculate subtotal
            transaction.Subtotal = transaction.Items.Sum(i => i.TotalAmount);

            // Calculate discount amount
            if (transaction.DiscountPercentage > 0)
            {
                transaction.DiscountAmount = Math.Round(transaction.Subtotal * (transaction.DiscountPercentage / 100), 2);
            }

            // Calculate tax amount
            if (transaction.TaxPercentage > 0)
            {
                transaction.TaxAmount = Math.Round((transaction.Subtotal - transaction.DiscountAmount) * (transaction.TaxPercentage / 100), 2);
            }

            // Calculate total
            transaction.Total = transaction.Subtotal - transaction.DiscountAmount + transaction.TaxAmount;

            // Calculate remaining balance
            transaction.RemainingBalance = Math.Max(0, transaction.Total - transaction.PaidAmount);
        }
    }
}