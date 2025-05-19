// OfflinePOS.DataAccess/Services/DatabaseInitializer.cs
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OfflinePOS.DataAccess.Services
{
    /// <summary>
    /// Service responsible for initializing the database and seeding initial data
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly ApplicationDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the DatabaseInitializer class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="dbContext">Database context</param>
        public DatabaseInitializer(
            IServiceProvider serviceProvider,
            ILogger<DatabaseInitializer> logger,
            ApplicationDbContext dbContext)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Initializes the database, creates it if it doesn't exist, and seeds initial data
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // For development purposes, delete and recreate the database
                // This ensures a clean schema without migration conflicts
                await _dbContext.Database.EnsureDeletedAsync();
                _logger.LogInformation("Existing database deleted.");

                // Create a new database with the updated schema
                await _dbContext.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database created successfully with updated schema.");

                // Seed initial data
                await SeedInitialDataAsync();

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        /// <summary>
        /// Check if required tables exist in the database
        /// </summary>
        private async Task<bool> CheckTablesExistAsync()
        {
            try
            {
                // Try to get a count from the Users table - if it succeeds, the table exists
                await _dbContext.Users.CountAsync();
                return true;
            }
            catch (SqlException)
            {
                // If we get an exception, the table doesn't exist
                return false;
            }
        }

        /// <summary>
        /// Verifies the database schema exists correctly
        /// </summary>
        private async Task VerifyDatabaseSchemaAsync()
        {
            _logger.LogInformation("Verifying database schema...");

            // Check if Users table exists
            var userTableExistsQuery = await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users') THEN 1 ELSE 0 END");

            bool usersTableExists = userTableExistsQuery == 1;

            if (!usersTableExists)
            {
                _logger.LogWarning("Users table does not exist! Attempting to create schema...");
                await _dbContext.Database.EnsureDeletedAsync();
                await _dbContext.Database.EnsureCreatedAsync();
                _logger.LogInformation("Schema recreated successfully.");
            }
            else
            {
                _logger.LogInformation("Database schema verification completed successfully.");
            }
        }

        /// <summary>
        /// Seeds the database with initial required data
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task SeedInitialDataAsync()
        {
            // Seed admin user if not exists
            if (!await _dbContext.Users.AnyAsync())
            {
                _logger.LogInformation("Seeding initial admin user...");

                // Generate salt and hash for password
                var salt = GenerateSalt();
                var passwordHash = HashPassword("Admin@123", salt);

                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = passwordHash,
                    PasswordSalt = Convert.ToBase64String(salt),
                    FullName = "System Administrator",
                    Role = "Admin",
                    JobTitle = "Administrator",
                    CreatedById = 1, // Self-reference as there's no other user yet
                    IsActive = true
                };

                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Admin user created successfully");
            }

            // Seed cashier user for testing
            if (!await _dbContext.Users.AnyAsync(u => u.Role == "Cashier"))
            {
                _logger.LogInformation("Seeding test cashier user...");

                var salt = GenerateSalt();
                var passwordHash = HashPassword("Cashier@123", salt);

                var cashierUser = new User
                {
                    Username = "cashier",
                    PasswordHash = passwordHash,
                    PasswordSalt = Convert.ToBase64String(salt),
                    FullName = "Test Cashier",
                    Role = "Cashier",
                    JobTitle = "Cashier",
                    CreatedById = 1, // Created by admin
                    IsActive = true
                };

                _dbContext.Users.Add(cashierUser);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Cashier user created successfully");
            }

            // Seed company settings if not exists
            if (!await _dbContext.CompanySettings.AnyAsync())
            {
                _logger.LogInformation("Seeding initial company settings...");

                var companySettings = new CompanySetting
                {
                    CompanyName = "POS System",
                    Address = "Default Address",
                    PhoneNumber1 = "123-456-7890",
                    PhoneNumber2 = "",  // Empty string instead of NULL
                    Logo = new byte[0], // Empty byte array instead of NULL
                    MainCurrency = "USD",
                    DefaultLanguage = "en-US",
                    DollarRate = 1.0m,
                    CreatedById = 1,    // Admin user
                    IsActive = true     // Don't forget this field
                };

                _dbContext.CompanySettings.Add(companySettings);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Company settings created successfully");
            }

            // Seed basic product categories
            if (!await _dbContext.Categories.AnyAsync())
            {
                _logger.LogInformation("Seeding basic product categories...");

                var categories = new[]
                {
                    new Category
                    {
                        Name = "Beverages",
                        Type = "Product",
                        Description = "Drinks and liquid refreshments",
                        CreatedById = 1,
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Food",
                        Type = "Product",
                        Description = "Edible items and snacks",
                        CreatedById = 1,
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Electronics",
                        Type = "Product",
                        Description = "Electronic devices and accessories",
                        CreatedById = 1,
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Utilities",
                        Type = "Expense",
                        Description = "Utility expenses like electricity, water, etc.",
                        CreatedById = 1,
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Rent",
                        Type = "Expense",
                        Description = "Rent and space-related expenses",
                        CreatedById = 1,
                        IsActive = true
                    }
                };

                _dbContext.Categories.AddRange(categories);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Product categories created successfully");
            }
            if (!await _dbContext.Suppliers.AnyAsync())
            {
                _logger.LogInformation("Seeding sample suppliers...");

                var suppliers = new[]
                {
        new Supplier
        {
            Name = "TechSupply Inc.",
            PhoneNumber = "123-456-7890",
            Address = "123 Tech St, Innovation City",
            Email = "contact@techsupply.example",
            ContactPerson = "John Smith",
            PaymentTerms = "Net 30",
            CurrentBalance = 0,
            TaxId = "TS-12345678", // Added TaxId value
            CreatedById = 1,
            IsActive = true
        },
        new Supplier
        {
            Name = "Global Foods Ltd.",
            PhoneNumber = "098-765-4321",
            Address = "456 Food Ave, Culinary Town",
            Email = "orders@globalfoods.example",
            ContactPerson = "Jane Doe",
            PaymentTerms = "COD",
            CurrentBalance = 0,
            TaxId = "GF-87654321", // Added TaxId value
            CreatedById = 1,
            IsActive = true
        },
        new Supplier
        {
            Name = "Office Supplies Co.",
            PhoneNumber = "111-222-3333",
            Address = "789 Office Blvd, Business District",
            Email = "sales@officesupplies.example",
            ContactPerson = "Robert Johnson",
            PaymentTerms = "Net 15",
            CurrentBalance = 0,
            TaxId = "OS-11223344", // Added TaxId value
            CreatedById = 1,
            IsActive = true
        }
    };

                _dbContext.Suppliers.AddRange(suppliers);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Sample suppliers created successfully");
            }
        }

        /// <summary>
        /// Generates a random salt for password hashing
        /// </summary>
        /// <returns>Random salt as byte array</returns>
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Hashes a password with the given salt using PBKDF2
        /// </summary>
        /// <param name="password">Clear text password</param>
        /// <param name="salt">Salt for hashing</param>
        /// <returns>Hashed password as Base64 string</returns>
        private string HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }
    }
}