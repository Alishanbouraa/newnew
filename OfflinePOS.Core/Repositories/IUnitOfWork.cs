// File: OfflinePOS.Core/Repositories/IUnitOfWork.cs
using OfflinePOS.Core.Models;
using System;
using System.Threading.Tasks;

namespace OfflinePOS.Core.Repositories
{
    /// <summary>
    /// Unit of Work interface for managing transactions and repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties
        IRepository<User> Users { get; }
        IRepository<Product> Products { get; }
        IRepository<Category> Categories { get; }
        IRepository<Customer> Customers { get; }
        IRepository<Transaction> Transactions { get; }
        IRepository<TransactionItem> TransactionItems { get; }
        IRepository<DrawerOperation> DrawerOperations { get; }
        IRepository<Stock> Stocks { get; }
        IRepository<StockAdjustment> StockAdjustments { get; }
        IRepository<Supplier> Suppliers { get; }
        IRepository<DrawerTransaction> DrawerTransactions { get; }
        IRepository<CompanySetting> CompanySettings { get; }

        // New repository properties for supplier invoice functionality
        IRepository<SupplierInvoice> SupplierInvoices { get; }
        IRepository<SupplierInvoiceItem> SupplierInvoiceItems { get; }
        IRepository<SupplierPayment> SupplierPayments { get; }

        // Transaction methods
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();

        // Async transaction methods
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Save changes method
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Executes a database operation with retry logic
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <returns>The result of the operation</returns>
        Task<TResult> ExecuteWithStrategyAsync<TResult>(Func<Task<TResult>> operation);

        /// <summary>
        /// Executes a database operation with retry logic
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ExecuteWithStrategyAsync(Func<Task> operation);
    }
}