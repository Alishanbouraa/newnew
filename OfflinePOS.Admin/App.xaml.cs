// OfflinePOS.Admin/App.xaml.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Diagnostics;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Services;
using OfflinePOS.Core.ViewModels;
using OfflinePOS.DataAccess;
using OfflinePOS.DataAccess.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace OfflinePOS.Admin
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private IConfiguration _configuration;
        private ILogger<App> _logger;
        private User _currentUser;

        protected override async void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();

            // Configure XAML diagnostics
            ConfigureXamlDiagnostics();

            try
            {
                // Initialize database before showing the login window
                _logger.LogInformation("Initializing application...");
                var dbInitializer = _serviceProvider.GetRequiredService<DatabaseInitializer>();
                await dbInitializer.InitializeDatabaseAsync();

                ShowLoginWindow();

                _logger.LogInformation("Application started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error during application startup");
                MessageBox.Show($"Failed to start application: {ex.Message}\n\nPlease check your database connection and try again.",
                                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register logging
            services.AddLogging(configure =>
            {
                configure.AddDebug();
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Register configuration
            services.AddSingleton(_configuration);

            // Register DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    _configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }),
                    ServiceLifetime.Transient);

            // Register database initializer
            services.AddTransient<DatabaseInitializer>();

            // Register services
            services.AddTransient<IAuthService, AuthService>();

            // Register windows - only the LoginView
            services.AddTransient<LoginView>();

            // Register ViewModels
            services.AddTransient(provider =>
                new LoginViewModel(
                    provider.GetRequiredService<IAuthService>(),
                    user =>
                    {
                        _currentUser = user;
                        ShowMainWindow();
                    }));
        }

        private void ConfigureXamlDiagnostics()
        {
            // Enable XAML diagnostic tracing
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(
                new XamlTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }

        private void ShowLoginWindow()
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginView>();
            loginWindow.Show();
        }

        private void ShowMainWindow()
        {
            try
            {
                // Close all windows (login window)
                foreach (Window window in Current.Windows)
                {
                    if (window is LoginView)
                    {
                        window.Close();
                    }
                }

                // Set current user in the DbContext
                var dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.CurrentUserId = _currentUser.Id;

                // Create and show main window with the current user and service provider
                _logger.LogInformation($"Creating main window for user: {_currentUser.Username}");
                var mainWindow = new MainWindow(_currentUser, _serviceProvider);
                mainWindow.Show();

                _logger.LogInformation($"Main window opened for user: {_currentUser.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing main window after login");
                MessageBox.Show($"Error opening main application window: {ex.Message}\n\nPlease restart the application.",
                                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
    }
}