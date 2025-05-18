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

            // You could add more decimal precision configurations here if needed
            // For example, if there are other decimal properties in the model

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