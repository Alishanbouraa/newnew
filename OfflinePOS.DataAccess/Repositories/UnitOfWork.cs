// OfflinePOS.DataAccess/Repositories/UnitOfWork.cs
using Microsoft.EntityFrameworkCore.Storage;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using System;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Repositories
{
    /// <summary>
    /// Unit of Work implementation for managing transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;
        private bool _disposed = false;

        private IRepository<User> _users;
        private IRepository<Product> _products;
        private IRepository<Category> _categories;
        private IRepository<Customer> _customers;
        private IRepository<Transaction> _transactions;
        private IRepository<TransactionItem> _transactionItems;
        private IRepository<DrawerOperation> _drawerOperations;
        private IRepository<DrawerTransaction> _drawerTransactions;
        private IRepository<CompanySetting> _companySettings;

        /// <summary>
        /// Initializes a new instance of the UnitOfWork class
        /// </summary>
        /// <param name="context">Database context</param>
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public IRepository<User> Users => _users ??= new Repository<User>(_context);

        /// <inheritdoc/>
        public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
        // OfflinePOS.DataAccess/Repositories/UnitOfWork.cs - Add these properties and initialization

        private IRepository<Stock> _stocks;
        private IRepository<StockAdjustment> _stockAdjustments;

        /// <inheritdoc/>
        public IRepository<Stock> Stocks => _stocks ??= new Repository<Stock>(_context);

        /// <inheritdoc/>
        public IRepository<StockAdjustment> StockAdjustments => _stockAdjustments ??= new Repository<StockAdjustment>(_context);
        /// <inheritdoc/>
        public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);

        /// <inheritdoc/>
        public IRepository<Customer> Customers => _customers ??= new Repository<Customer>(_context);

        /// <inheritdoc/>
        public IRepository<Transaction> Transactions => _transactions ??= new Repository<Transaction>(_context);

        /// <inheritdoc/>
        public IRepository<TransactionItem> TransactionItems => _transactionItems ??= new Repository<TransactionItem>(_context);

        /// <inheritdoc/>
        public IRepository<DrawerOperation> DrawerOperations => _drawerOperations ??= new Repository<DrawerOperation>(_context);

        /// <inheritdoc/>
        public IRepository<DrawerTransaction> DrawerTransactions => _drawerTransactions ??= new Repository<DrawerTransaction>(_context);

        /// <inheritdoc/>
        public IRepository<CompanySetting> CompanySettings => _companySettings ??= new Repository<CompanySetting>(_context);

        /// <inheritdoc/>
        public void BeginTransaction()
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already in progress");

            _transaction = _context.Database.BeginTransaction();
        }

        /// <inheritdoc/>
        public void CommitTransaction()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction is in progress");

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

        /// <inheritdoc/>
        public void RollbackTransaction()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction is in progress");

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

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
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
                }

                _disposed = true;
            }
        }
    }
}