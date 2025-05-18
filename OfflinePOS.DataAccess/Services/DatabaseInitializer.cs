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

                // Check if database exists and can connect
                bool dbExists = await _dbContext.Database.CanConnectAsync();
                _logger.LogInformation($"Database exists: {dbExists}");

                if (!dbExists)
                {
                    _logger.LogInformation("Creating database...");
                    // Create the database
                    await _dbContext.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Database created successfully.");
                }
                else
                {
                    // Verify database structure even if it exists
                    bool tablesExist = await CheckTablesExistAsync();

                    if (!tablesExist)
                    {
                        _logger.LogWarning("Database exists but tables are missing. Creating schema...");
                        await _dbContext.Database.EnsureCreatedAsync();
                        _logger.LogInformation("Schema created successfully.");
                    }
                }

                // Verify pending migrations
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                int pendingCount = pendingMigrations.Count();
                if (pendingCount > 0)
                {
                    _logger.LogInformation($"Applying {pendingCount} pending migrations...");
                    await _dbContext.Database.MigrateAsync();
                    _logger.LogInformation("All migrations applied successfully.");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found.");
                }

                // Verify database schema
                await VerifyDatabaseSchemaAsync();

                // Seed initial data if needed
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
                    JobTitle = "Administrator", // Add this line to provide JobTitle value
                    CreatedById = 1, // Self-reference as there's no other user yet
                    IsActive = true
                };

                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Admin user created successfully");
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