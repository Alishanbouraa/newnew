// OfflinePOS.Core/Repositories/IUnitOfWork.cs
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
        /// <summary>
        /// Repository for User entities
        /// </summary>
        IRepository<User> Users { get; }

        /// <summary>
        /// Repository for Product entities
        /// </summary>
        IRepository<Product> Products { get; }

        /// <summary>
        /// Repository for Category entities
        /// </summary>
        IRepository<Category> Categories { get; }

        /// <summary>
        /// Repository for Customer entities
        /// </summary>
        IRepository<Customer> Customers { get; }

        /// <summary>
        /// Repository for Transaction entities
        /// </summary>
        IRepository<Transaction> Transactions { get; }

        /// <summary>
        /// Repository for TransactionItem entities
        /// </summary>
        IRepository<TransactionItem> TransactionItems { get; }

        /// <summary>
        /// Repository for DrawerOperation entities
        /// </summary>
        IRepository<DrawerOperation> DrawerOperations { get; }

        /// <summary>
        /// Repository for DrawerTransaction entities
        /// </summary>
        IRepository<DrawerTransaction> DrawerTransactions { get; }

        /// <summary>
        /// Repository for CompanySetting entities
        /// </summary>
        IRepository<CompanySetting> CompanySettings { get; }

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// Saves all changes made through this unit of work to the database
        /// </summary>
        /// <returns>Number of state entries written to the database</returns>
        Task<int> SaveChangesAsync();
    }
}