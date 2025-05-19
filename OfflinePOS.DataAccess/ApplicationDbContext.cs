// File: OfflinePOS.DataAccess/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using OfflinePOS.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess
{
    /// <summary>
    /// Entity Framework Database Context for the POS application
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Current user ID for auditing purposes
        /// </summary>
        public int CurrentUserId { get; set; } = 1; // Default to admin for initial operations

        /// <summary>
        /// Company settings
        /// </summary>
        public DbSet<CompanySetting> CompanySettings { get; set; }

        /// <summary>
        /// Users/employees
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Categories
        /// </summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>
        /// Products
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// Stock inventory
        /// </summary>
        public DbSet<Stock> Stocks { get; set; }

        /// <summary>
        /// Stock adjustment transactions
        /// </summary>
        public DbSet<StockAdjustment> StockAdjustments { get; set; }

        /// <summary>
        /// Customers
        /// </summary>
        public DbSet<Customer> Customers { get; set; }

        /// <summary>
        /// Transactions
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// Transaction items
        /// </summary>
        public DbSet<TransactionItem> TransactionItems { get; set; }

        /// <summary>
        /// Drawer operations
        /// </summary>
        public DbSet<DrawerOperation> DrawerOperations { get; set; }

        /// <summary>
        /// Drawer transactions
        /// </summary>
        public DbSet<DrawerTransaction> DrawerTransactions { get; set; }

        /// <summary>
        /// Suppliers
        /// </summary>
        public DbSet<Supplier> Suppliers { get; set; }

        /// <summary>
        /// Supplier invoices
        /// </summary>
        public DbSet<SupplierInvoice> SupplierInvoices { get; set; }

        /// <summary>
        /// Supplier invoice items
        /// </summary>
        public DbSet<SupplierInvoiceItem> SupplierInvoiceItems { get; set; }

        /// <summary>
        /// Supplier payments
        /// </summary>
        public DbSet<SupplierPayment> SupplierPayments { get; set; }

        /// <summary>
        /// Creates a new instance of ApplicationDbContext
        /// </summary>
        /// <param name="options">The options to be used by the DbContext</param>
        public ApplicationDbContext(DbContextOptionsBuilder<ApplicationDbContext> options)
            : base(options.Options)
        {
        }

        /// <summary>
        /// Creates a new instance of ApplicationDbContext
        /// </summary>
        /// <param name="options">The options to be used by the DbContext</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Configures the database connection
        /// </summary>
        /// <param name="optionsBuilder">Options builder</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Default connection string if none provided
                optionsBuilder.UseSqlServer(
                    @"Server=.\posserver;Database=OfflinePOSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
            }

            // Enable sensitive data logging in development
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
#endif

            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// Configures the database model
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Explicitly configure the User entity to map to the Users table
            modelBuilder.Entity<User>().ToTable("Users");

            // Configure entities using fluent API
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure RowVersion for concurrency control
            modelBuilder.Entity<User>()
                .Property(u => u.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for User entity
            modelBuilder.Entity<User>()
                .Property(u => u.MonthlySalary)
                .HasColumnType("decimal(18,2)");

            // Configure CompanySetting entity
            modelBuilder.Entity<CompanySetting>()
                .ToTable("CompanySettings")
                .Property(c => c.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for CompanySetting entity
            modelBuilder.Entity<CompanySetting>()
                .Property(c => c.DollarRate)
                .HasColumnType("decimal(18,4)");

            // Configure Product entity
            modelBuilder.Entity<Product>()
                .ToTable("Products")
                .Property(p => p.RowVersion)
                .IsRowVersion();

            // Configure Description and Dimensions to be optional
            modelBuilder.Entity<Product>()
                .Property(p => p.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            modelBuilder.Entity<Product>()
                .Property(p => p.Dimensions)
                .IsRequired(false)
                .HasMaxLength(50);

            // Configure decimal precision for Product entity
            modelBuilder.Entity<Product>()
                .Property(p => p.BoxPurchasePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.BoxWholesalePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.BoxSalePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.ItemPurchasePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.ItemWholesalePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.ItemSalePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.MSRP)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.Weight)
                .HasColumnType("decimal(18,2)");

            // Configure Category entity
            modelBuilder.Entity<Category>()
                .ToTable("Categories")
                .Property(c => c.RowVersion)
                .IsRowVersion();

            // Configure Customer entity
            modelBuilder.Entity<Customer>()
                .ToTable("Customers")
                .Property(c => c.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for Customer entity
            modelBuilder.Entity<Customer>()
                .Property(c => c.CurrentBalance)
                .HasColumnType("decimal(18,2)");

            // Configure Transaction entity
            modelBuilder.Entity<Transaction>()
                .ToTable("Transactions")
                .Property(t => t.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for Transaction entity
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Subtotal)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.DiscountPercentage)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.DiscountAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.TaxPercentage)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.TaxAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Total)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.PaidAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Transaction>()
                .Property(t => t.RemainingBalance)
                .HasColumnType("decimal(18,2)");

            // Configure TransactionItem entity
            modelBuilder.Entity<TransactionItem>()
                .ToTable("TransactionItems")
                .Property(ti => ti.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for TransactionItem entity
            modelBuilder.Entity<TransactionItem>()
                .Property(ti => ti.UnitPrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransactionItem>()
                .Property(ti => ti.DiscountPercentage)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransactionItem>()
                .Property(ti => ti.DiscountAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransactionItem>()
                .Property(ti => ti.TaxPercentage)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransactionItem>()
                .Property(ti => ti.TaxAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransactionItem>()
                .Property(ti => ti.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // Configure DrawerOperation entity
            modelBuilder.Entity<DrawerOperation>()
                .ToTable("DrawerOperations")
                .Property(d => d.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for DrawerOperation entity
            modelBuilder.Entity<DrawerOperation>()
                .Property(d => d.StartingBalance)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DrawerOperation>()
                .Property(d => d.EndingBalance)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DrawerOperation>()
                .Property(d => d.ExpectedBalance)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DrawerOperation>()
                .Property(d => d.Difference)
                .HasColumnType("decimal(18,2)");

            // Configure DrawerTransaction entity
            modelBuilder.Entity<DrawerTransaction>()
                .ToTable("DrawerTransactions")
                .Property(dt => dt.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for DrawerTransaction entity
            modelBuilder.Entity<DrawerTransaction>()
                .Property(dt => dt.Amount)
                .HasColumnType("decimal(18,2)");

            // Configure Stock entity
            modelBuilder.Entity<Stock>()
                .ToTable("Stocks")
                .Property(s => s.RowVersion)
                .IsRowVersion();

            // Configure LocationCode to be optional (modification to fix NULL error)
            modelBuilder.Entity<Stock>()
                .Property(s => s.LocationCode)
                .IsRequired(false)
                .HasMaxLength(50);

            // Configure StockAdjustment entity
            modelBuilder.Entity<StockAdjustment>()
                .ToTable("StockAdjustments")
                .Property(sa => sa.RowVersion)
                .IsRowVersion();

            // Configure Supplier entity
            modelBuilder.Entity<Supplier>()
                .ToTable("Suppliers")
                .Property(s => s.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for Supplier entity
            modelBuilder.Entity<Supplier>()
                .Property(s => s.CurrentBalance)
                .HasColumnType("decimal(18,2)");

            // Configure SupplierInvoice entity
            modelBuilder.Entity<SupplierInvoice>()
                .ToTable("SupplierInvoices")
                .Property(si => si.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for SupplierInvoice entity
            modelBuilder.Entity<SupplierInvoice>()
                .Property(si => si.TotalAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SupplierInvoice>()
                .Property(si => si.PaidAmount)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SupplierInvoice>()
                .Property(si => si.RemainingBalance)
                .HasColumnType("decimal(18,2)");

            // Configure SupplierInvoiceItem entity
            modelBuilder.Entity<SupplierInvoiceItem>()
                .ToTable("SupplierInvoiceItems")
                .Property(sii => sii.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for SupplierInvoiceItem entity
            modelBuilder.Entity<SupplierInvoiceItem>()
                .Property(sii => sii.BoxPurchasePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SupplierInvoiceItem>()
                .Property(sii => sii.ItemPurchasePrice)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SupplierInvoiceItem>()
                .Property(sii => sii.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // Configure SupplierPayment entity
            modelBuilder.Entity<SupplierPayment>()
                .ToTable("SupplierPayments")
                .Property(sp => sp.RowVersion)
                .IsRowVersion();

            // Configure decimal precision for SupplierPayment entity
            modelBuilder.Entity<SupplierPayment>()
                .Property(sp => sp.Amount)
                .HasColumnType("decimal(18,2)");

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Product-Supplier relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Product-SupplierInvoice relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.SupplierInvoice)
                .WithMany()
                .HasForeignKey(p => p.SupplierInvoiceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.DrawerOperation)
                .WithMany(d => d.Transactions)
                .HasForeignKey(t => t.DrawerOperationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Transaction)
                .WithMany(t => t.Items)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Product)
                .WithMany()
                .HasForeignKey(ti => ti.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DrawerOperation>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DrawerTransaction>()
                .HasOne(dt => dt.DrawerOperation)
                .WithMany(d => d.DrawerTransactions)
                .HasForeignKey(dt => dt.DrawerOperationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DrawerTransaction>()
                .HasOne(dt => dt.RecordedBy)
                .WithMany()
                .HasForeignKey(dt => dt.RecordedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Stock relationships
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Product)
                .WithOne(p => p.Stock)
                .HasForeignKey<Stock>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(sa => sa.Product)
                .WithMany(p => p.StockAdjustments)
                .HasForeignKey(sa => sa.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationships for supplier invoices
            modelBuilder.Entity<SupplierInvoice>()
                .HasOne(si => si.Supplier)
                .WithMany()
                .HasForeignKey(si => si.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierInvoiceItem>()
                .HasOne(sii => sii.Invoice)
                .WithMany(si => si.Items)
                .HasForeignKey(sii => sii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupplierInvoiceItem>()
                .HasOne(sii => sii.Product)
                .WithMany()
                .HasForeignKey(sii => sii.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierPayment>()
                .HasOne(sp => sp.Supplier)
                .WithMany()
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierPayment>()
                .HasOne(sp => sp.Invoice)
                .WithMany(si => si.Payments)
                .HasForeignKey(sp => sp.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierPayment>()
                .HasOne(sp => sp.ProcessedBy)
                .WithMany()
                .HasForeignKey(sp => sp.ProcessedById)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Saves all changes made in this context to the database with audit information
        /// </summary>
        /// <returns>The number of state entries written to the database</returns>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Saves all changes made in this context to the database with audit information asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task that represents the asynchronous save operation</returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates audit fields for changed entities
        /// </summary>
        private void UpdateAuditFields()
        {
            var now = DateTime.Now;

            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = now;
                    entry.Entity.CreatedById = CurrentUserId;
                    entry.Entity.IsActive = true;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastUpdatedDate = now;
                    entry.Entity.LastUpdatedById = CurrentUserId;

                    // Don't modify CreatedDate and CreatedById
                    entry.Property(p => p.CreatedDate).IsModified = false;
                    entry.Property(p => p.CreatedById).IsModified = false;
                }
            }
        }
    }
}