using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace OfflinePOS.DataAccess
{
    /// <summary>
    /// Factory for creating DbContext instances used by EF Core tools
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        /// <summary>
        /// Creates a new DbContext instance
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>A new DbContext instance</returns>
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Try to load connection string from appsettings.json if it exists
            string connectionString = @"Server=.\posserver;Database=OfflinePOSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build();

                connectionString = configuration.GetConnectionString("DefaultConnection") ?? connectionString;
            }
            catch
            {
                // Use default connection string if appsettings.json not found or cannot be read
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}