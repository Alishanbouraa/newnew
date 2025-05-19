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
        // Repository properties (unchanged)
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

        // Existing transaction methods
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();

        // New async transaction methods
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Save changes method (unchanged)
        Task<int> SaveChangesAsync();
    }
}