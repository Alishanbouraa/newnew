// File: OfflinePOS.DataAccess/Repositories/UnitOfWork.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Repositories
{
    /// <summary>
    /// Thread-safe Unit of Work implementation for managing transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly object _transactionLock = new object();
        private IDbContextTransaction _transaction;
        private bool _disposed = false;

        // Thread-safe repository cache
        private readonly ConcurrentDictionary<Type, object> _repositories = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the UnitOfWork class
        /// </summary>
        /// <param name="context">Database context</param>
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Repository properties with lazy initialization
        public IRepository<User> Users => GetRepository<User>();
        public IRepository<Product> Products => GetRepository<Product>();
        public IRepository<Stock> Stocks => GetRepository<Stock>();
        public IRepository<StockAdjustment> StockAdjustments => GetRepository<StockAdjustment>();
        public IRepository<Supplier> Suppliers => GetRepository<Supplier>();
        public IRepository<Category> Categories => GetRepository<Category>();
        public IRepository<Customer> Customers => GetRepository<Customer>();
        public IRepository<Transaction> Transactions => GetRepository<Transaction>();
        public IRepository<TransactionItem> TransactionItems => GetRepository<TransactionItem>();
        public IRepository<DrawerOperation> DrawerOperations => GetRepository<DrawerOperation>();
        public IRepository<DrawerTransaction> DrawerTransactions => GetRepository<DrawerTransaction>();
        public IRepository<CompanySetting> CompanySettings => GetRepository<CompanySetting>();
        public IRepository<SupplierInvoice> SupplierInvoices => GetRepository<SupplierInvoice>();
        public IRepository<SupplierInvoiceItem> SupplierInvoiceItems => GetRepository<SupplierInvoiceItem>();
        public IRepository<SupplierPayment> SupplierPayments => GetRepository<SupplierPayment>();

        /// <summary>
        /// Gets a repository of the specified type, creating it if necessary
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>Repository instance</returns>
        private IRepository<T> GetRepository<T>() where T : class
        {
            return (IRepository<T>)_repositories.GetOrAdd(
                typeof(T),
                _ => new Repository<T>(_context));
        }

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        public void BeginTransaction()
        {
            lock (_transactionLock)
            {
                if (_transaction == null)
                {
                    _transaction = _context.Database.BeginTransaction();
                }
            }
        }

        /// <summary>
        /// Begins a database transaction asynchronously
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            lock (_transactionLock)
            {
                if (_transaction == null)
                {
                    // Note: We need to use synchronous approach within lock
                    _transaction = _context.Database.BeginTransactionAsync().GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public void CommitTransaction()
        {
            lock (_transactionLock)
            {
                if (_transaction == null)
                    return;

                try
                {
                    _transaction.Commit();
                }
                finally
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        /// <summary>
        /// Commits the current transaction asynchronously
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            IDbContextTransaction transactionToCommit = null;

            lock (_transactionLock)
            {
                if (_transaction == null)
                    return;

                transactionToCommit = _transaction;
                _transaction = null;
            }

            try
            {
                await transactionToCommit.CommitAsync();
            }
            finally
            {
                await transactionToCommit.DisposeAsync();
            }
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public void RollbackTransaction()
        {
            lock (_transactionLock)
            {
                if (_transaction == null)
                    return;

                try
                {
                    _transaction.Rollback();
                }
                finally
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        /// <summary>
        /// Rolls back the current transaction asynchronously
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            IDbContextTransaction transactionToRollback = null;

            lock (_transactionLock)
            {
                if (_transaction == null)
                    return;

                transactionToRollback = _transaction;
                _transaction = null;
            }

            try
            {
                await transactionToRollback.RollbackAsync();
            }
            finally
            {
                await transactionToRollback.DisposeAsync();
            }
        }

        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        /// <returns>Number of state entries written to the database</returns>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Executes a database operation with retry logic
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <returns>The result of the operation</returns>
        public async Task<TResult> ExecuteWithStrategyAsync<TResult>(Func<Task<TResult>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(operation);
        }

        /// <summary>
        /// Executes a database operation with retry logic
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ExecuteWithStrategyAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(operation);
        }

        /// <summary>
        /// Releases all resources used by the UnitOfWork
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the UnitOfWork
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_transactionLock)
                    {
                        if (_transaction != null)
                        {
                            _transaction.Dispose();
                            _transaction = null;
                        }
                    }

                    _context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}