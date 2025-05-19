// File: OfflinePOS.DataAccess/Repositories/UnitOfWork.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Repositories
{
    /// <summary>
    /// Thread-safe Unit of Work implementation for managing transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;
        private bool _disposed = false;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // Repository instances
        private readonly ConcurrentDictionary<Type, object> _repositories = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the UnitOfWork class
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="serviceProvider">Service provider for creating repositories</param>
        public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
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

        // New repository properties for supplier invoice functionality
        public IRepository<SupplierInvoice> SupplierInvoices => GetRepository<SupplierInvoice>();
        public IRepository<SupplierInvoiceItem> SupplierInvoiceItems => GetRepository<SupplierInvoiceItem>();
        public IRepository<SupplierPayment> SupplierPayments => GetRepository<SupplierPayment>();

        /// <summary>
        /// Gets a repository of the specified type, creating it if necessary
        /// </summary>
        private IRepository<T> GetRepository<T>() where T : class
        {
            return (IRepository<T>)_repositories.GetOrAdd(
                typeof(T),
                _ => new Repository<T>(_context));
        }

        /// <inheritdoc/>
        public void BeginTransaction()
        {
            _lock.Wait();
            try
            {
                _transaction = _transaction ?? _context.Database.BeginTransaction();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task BeginTransactionAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _transaction = _transaction ?? await _context.Database.BeginTransactionAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void CommitTransaction()
        {
            _lock.Wait();
            try
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
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CommitTransactionAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_transaction == null)
                    return;

                try
                {
                    await _transaction.CommitAsync();
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void RollbackTransaction()
        {
            _lock.Wait();
            try
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
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RollbackTransactionAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_transaction == null)
                    return;

                try
                {
                    await _transaction.RollbackAsync();
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<TResult> ExecuteWithStrategyAsync<TResult>(Func<Task<TResult>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(operation);
        }

        /// <inheritdoc/>
        public async Task ExecuteWithStrategyAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(operation);
        }

        /// <summary>
        /// Disposes the context and resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the context and resources
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release the lock
                    _lock.Dispose();

                    // Dispose transaction if active
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                        _transaction = null;
                    }

                    // Dispose context
                    _context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}