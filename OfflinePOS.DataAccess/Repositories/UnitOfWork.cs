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
        public async Task BeginTransactionAsync()
        {
            await _lock.WaitAsync();

            try
            {
                if (_transaction != null)
                    throw new InvalidOperationException("A transaction is already in progress");

                _transaction = await _context.Database.BeginTransactionAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void BeginTransaction()
        {
            _lock.Wait();

            try
            {
                if (_transaction != null)
                    throw new InvalidOperationException("A transaction is already in progress");

                _transaction = _context.Database.BeginTransaction();
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
                    throw new InvalidOperationException("No transaction is in progress");

                await _transaction.CommitAsync();
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

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
                    throw new InvalidOperationException("No transaction is in progress");

                _transaction.Commit();
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

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
                    throw new InvalidOperationException("No transaction is in progress");

                await _transaction.RollbackAsync();
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

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
                    throw new InvalidOperationException("No transaction is in progress");

                _transaction.Rollback();
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync()
        {
            await _lock.WaitAsync();

            try
            {
                return await _context.SaveChangesAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by this instance
        /// </summary>
        /// <param name="disposing">True to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                    _lock.Dispose();
                }

                _disposed = true;
            }
        }
    }
}