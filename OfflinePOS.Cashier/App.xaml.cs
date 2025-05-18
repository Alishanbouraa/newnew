// OfflinePOS.Cashier/App.xaml.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Cashier.ViewModels;
using OfflinePOS.Cashier.Views;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using OfflinePOS.Core.ViewModels;
using OfflinePOS.DataAccess;
using OfflinePOS.DataAccess.Repositories;
using OfflinePOS.DataAccess.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OfflinePOS.Cashier
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private IConfiguration _configuration;
        private ILogger<App> _logger;
        private User _currentUser;

        /// <summary>
        /// Application startup
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Configure application shutdown mode to prevent automatic termination
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            EnsureResourcesLoaded();
            try
            {
                // Initialize database before showing the login window
                _logger.LogInformation("Initializing application...");
                var dbInitializer = _serviceProvider.GetRequiredService<DatabaseInitializer>();
                await dbInitializer.InitializeDatabaseAsync(); // This will recreate the database with proper schema

                // Show login window
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

        /// <summary>
        /// Configures application services
        /// </summary>
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

            // Register repositories and UoW
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            // Register services
            services.AddTransient<DatabaseInitializer>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<ITransactionService, TransactionService>();
            services.AddTransient<IDrawerService, DrawerService>();

            // Register ViewModels
            services.AddTransient(provider =>
                new LoginViewModel(
                    provider.GetRequiredService<IAuthService>(),
                    user =>
                    {
                        _currentUser = user;
                        ShowMainWindow();
                    }));

            services.AddTransient(provider =>
                new SalesViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<ITransactionService>(),
                    provider.GetRequiredService<IDrawerService>(),
                    provider.GetRequiredService<IAuthService>(),
                    provider.GetRequiredService<ILogger<SalesViewModel>>(),
                    _currentUser));

            services.AddTransient(provider =>
                new DrawerViewModel(
                    provider.GetRequiredService<IDrawerService>(),
                    provider.GetRequiredService<ILogger<DrawerViewModel>>(),
                    _currentUser));

            // Register views
            services.AddTransient<LoginView>();
            services.AddTransient<SalesView>();
            services.AddTransient<DrawerView>();

            // Note: We do NOT register MainWindow as a service since we need to manually create it with the current user
        }

        /// <summary>
        /// Shows the login window
        /// </summary>
        private void ShowLoginWindow()
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginView>();
            loginWindow.Show();
        }

        /// <summary>
        /// Shows the main application window after successful login
        /// </summary>
        private void ShowMainWindow()
        {
            try
            {
                // Close login window
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

                // Create and show main window - MANUALLY CREATE it with the required dependencies
                _logger.LogInformation($"Creating main window for user: {_currentUser.Username}");

                // Create main window with dependencies - don't use DI container to instantiate it
                var mainWindow = new MainWindow(
                    _serviceProvider,
                    _currentUser,
                    _serviceProvider.GetRequiredService<ILogger<MainWindow>>());

                // Set up navigation handlers
                var salesViewModel = _serviceProvider.GetRequiredService<SalesViewModel>();
                var drawerViewModel = _serviceProvider.GetRequiredService<DrawerViewModel>();

                salesViewModel.NavigationRequested += (sender, args) => {
                    if (args.ViewName == "DrawerView")
                        mainWindow.NavigateToView<DrawerView, DrawerViewModel>();
                };

                drawerViewModel.NavigationRequested += (sender, args) => {
                    _logger.LogInformation($"Navigation requested to: {args.ViewName}");

                    // Handle both "SalesView" and "Sales" to be more flexible
                    if (args.ViewName == "SalesView" || args.ViewName == "Sales")
                    {
                        mainWindow.NavigateToView<SalesView, SalesViewModel>();
                        _logger.LogInformation("Navigation to SalesView executed");
                    }
                    else if (args.ViewName == "Logout")
                    {
                        // Handle logout navigation if needed
                    }
                };

                // Set as application's main window
                Application.Current.MainWindow = mainWindow;
                Current.Properties["MainWindow"] = mainWindow;

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

        /// <summary>
        /// Ensures all required resources are loaded
        /// </summary>
        private void EnsureResourcesLoaded()
        {
            try
            {
                // Verify core resources are loaded
                var coreResourcePaths = new[]
                {
                    "pack://application:,,,/OfflinePOS.Core;component/Styles/CommonStyles.xaml",
                    "pack://application:,,,/OfflinePOS.Core;component/Converters/ConverterResources.xaml"
                };

                foreach (var resourcePath in coreResourcePaths)
                {
                    if (!Application.Current.Resources.MergedDictionaries.Any(d => d.Source?.ToString() == resourcePath))
                    {
                        var resourceDict = new ResourceDictionary { Source = new Uri(resourcePath, UriKind.Absolute) };
                        Application.Current.Resources.MergedDictionaries.Add(resourceDict);
                        _logger?.LogInformation($"Loaded resource dictionary: {resourcePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error ensuring resources are loaded. Some UI elements may not appear correctly.");
            }
        }
    }
}