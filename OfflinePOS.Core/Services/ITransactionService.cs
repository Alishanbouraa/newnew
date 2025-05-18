// OfflinePOS.Core/Services/ITransactionService.cs
using OfflinePOS.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Services
{
    /// <summary>
    /// Service for managing sales transactions
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Creates a new sales transaction
        /// </summary>
        /// <param name="transaction">Transaction to create</param>
        /// <returns>Created transaction</returns>
        Task<Transaction> CreateTransactionAsync(Transaction transaction);

        /// <summary>
        /// Gets a transaction by ID
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <returns>Transaction if found, null otherwise</returns>
        Task<Transaction> GetTransactionByIdAsync(int id);

        /// <summary>
        /// Gets a transaction by invoice number
        /// </summary>
        /// <param name="invoiceNumber">Invoice number</param>
        /// <returns>Transaction if found, null otherwise</returns>
        Task<Transaction> GetTransactionByInvoiceNumberAsync(string invoiceNumber);

        /// <summary>
        /// Gets transactions in a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Transactions in the date range</returns>
        Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets transactions for a specific customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>Customer's transactions</returns>
        Task<IEnumerable<Transaction>> GetTransactionsByCustomerAsync(int customerId);

        /// <summary>
        /// Gets transactions for a specific drawer operation
        /// </summary>
        /// <param name="drawerOperationId">Drawer operation ID</param>
        /// <returns>Drawer's transactions</returns>
        Task<IEnumerable<Transaction>> GetTransactionsByDrawerOperationAsync(int drawerOperationId);

        /// <summary>
        /// Updates a transaction's status
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="status">New status</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateTransactionStatusAsync(int transactionId, string status);

        /// <summary>
        /// Processes a payment for a transaction
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="amount">Payment amount</param>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>Updated transaction</returns>
        Task<Transaction> ProcessPaymentAsync(int transactionId, decimal amount, string paymentMethod);

        /// <summary>
        /// Generates a new invoice number
        /// </summary>
        /// <returns>Generated invoice number</returns>
        Task<string> GenerateInvoiceNumberAsync();
    }
}