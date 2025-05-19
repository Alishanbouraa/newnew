// File: OfflinePOS.Admin/App.xaml.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.ViewModels;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Diagnostics;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Repositories;
using OfflinePOS.Core.Services;
using OfflinePOS.Core.ViewModels;
using OfflinePOS.DataAccess;
using OfflinePOS.DataAccess.Repositories;
using OfflinePOS.DataAccess.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace OfflinePOS.Admin
{
    /// <summary>
    /// Main application class that handles initialization, dependency injection, and application lifecycle
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private IConfiguration _configuration;
        private ILogger<App> _logger;
        private User _currentUser;

        /// <summary>
        /// Provides access to the DI service provider throughout the application
        /// </summary>
        public IServiceProvider ServiceProvider => _serviceProvider;

        /// <summary>
        /// Application startup entry point
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

            // Configure XAML diagnostics
            ConfigureXamlDiagnostics();

            try
            {
                // Ensure resources are loaded first
                EnsureResourcesLoaded();

                // Initialize database before showing the login window
                _logger.LogInformation("Initializing application...");
                var dbInitializer = _serviceProvider.GetRequiredService<DatabaseInitializer>();
                await dbInitializer.InitializeDatabaseAsync(); // This will recreate the database with proper schema

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
        /// Configures application services for dependency injection
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

            // Register database initializer
            services.AddTransient<DatabaseInitializer>();

            // Register repositories and UoW
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IUnitOfWork>(provider =>
                new UnitOfWork(
                    provider.GetRequiredService<ApplicationDbContext>(),
                    provider));

            // Register core services
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<ICategoryService, CategoryService>();
            services.AddTransient<ISupplierService, SupplierService>();
            services.AddTransient<ISupplierInvoiceService, SupplierInvoiceService>();
            services.AddTransient<IStockService, StockService>();
            services.AddTransient<ICustomerService, CustomerService>();
            services.AddTransient<ITransactionService, TransactionService>();
            services.AddTransient<IDrawerService, DrawerService>();

            // Register ViewModels for Inventory Management
            services.AddTransient(provider =>
                new StockManagementViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<IStockService>(),
                    provider.GetRequiredService<ILogger<StockManagementViewModel>>(),
                    _currentUser));

            services.AddTransient(provider =>
                new BarcodeManagementViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<IStockService>(),
                    provider.GetRequiredService<ILogger<BarcodeManagementViewModel>>(),
                    _currentUser));

            services.AddTransient(provider =>
                new ProductImportExportViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<IStockService>(),
                    provider.GetRequiredService<ILogger<ProductImportExportViewModel>>(),
                    _currentUser));

            services.AddTransient(provider =>
                new ProductViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<IStockService>(),
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<ProductViewModel>>(),
                    _currentUser,
                    provider)); // Pass service provider for navigation

            // Register Category ViewModels
            services.AddTransient(provider =>
                new CategoryViewModel(
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ILogger<CategoryViewModel>>(),
                    _currentUser,
                    provider)); // Pass service provider for logger resolution

            // Register Supplier ViewModels
            services.AddTransient(provider =>
                new SupplierViewModel(
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<SupplierViewModel>>(),
                    _currentUser,
                    provider));

            // Register Supplier Invoice ViewModels
            services.AddTransient<Func<Supplier, SupplierInvoiceListViewModel>>(provider => (supplier) =>
                new SupplierInvoiceListViewModel(
                    provider.GetRequiredService<ISupplierInvoiceService>(),
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<SupplierInvoiceListViewModel>>(),
                    _currentUser,
                    provider,
                    supplier));

            // Register Customer ViewModels
            services.AddTransient(provider =>
                new CustomerViewModel(
                    provider.GetRequiredService<ICustomerService>(),
                    provider.GetRequiredService<ILogger<CustomerViewModel>>(),
                    _currentUser,
                    provider));

            // Register Transaction ViewModels
            services.AddTransient(provider =>
                new TransactionHistoryViewModel(
                    provider.GetRequiredService<ITransactionService>(),
                    provider.GetRequiredService<ILogger<TransactionHistoryViewModel>>(),
                    _currentUser));

            // Register ViewModel factories - Category
            services.AddTransient<Func<Category, bool, CategoryDialogViewModel>>(provider => (category, isNew) =>
                new CategoryDialogViewModel(
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ILogger<CategoryDialogViewModel>>(),
                    _currentUser,
                    category,
                    isNew));

            // Register ViewModel factories - Product
            services.AddTransient<Func<Product, ProductDialogViewModel>>(provider => (product) =>
                new ProductDialogViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<ProductDialogViewModel>>(),
                    _currentUser,
                    product));

            // Register ViewModel factories - Supplier
            services.AddTransient<Func<Supplier, SupplierDialogViewModel>>(provider => (supplier) =>
                new SupplierDialogViewModel(
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<SupplierDialogViewModel>>(),
                    _currentUser,
                    supplier));

            // Register ViewModel factories - Customer
            services.AddTransient<Func<Customer, CustomerDialogViewModel>>(provider => (customer) =>
                new CustomerDialogViewModel(
                    provider.GetRequiredService<ICustomerService>(),
                    provider.GetRequiredService<ILogger<CustomerDialogViewModel>>(),
                    _currentUser,
                    customer));

            services.AddTransient<Func<Customer, SettleDebtViewModel>>(provider => (customer) =>
                new SettleDebtViewModel(
                    provider.GetRequiredService<ICustomerService>(),
                    provider.GetRequiredService<ILogger<SettleDebtViewModel>>(),
                    _currentUser,
                    customer));

            // Register ViewModel factories - Transaction
            services.AddTransient<Func<Transaction, TransactionDetailsViewModel>>(provider => (transaction) =>
                new TransactionDetailsViewModel(
                    provider.GetRequiredService<ITransactionService>(),
                    provider.GetRequiredService<ILogger<TransactionDetailsViewModel>>(),
                    transaction));

            // Register inventory views
            services.AddTransient<StockManagementView>();
            services.AddTransient<BarcodeManagementView>();
            services.AddTransient<ProductImportExportView>();
            services.AddTransient<ProductView>();
            services.AddTransient<ProductDialogView>();

            // Register category views
            services.AddTransient<CategoryView>();
            services.AddTransient<CategoryDialogView>();

            // Register supplier views
            services.AddTransient<SupplierView>();
            services.AddTransient<SupplierDialogView>();
            services.AddTransient<SupplierInvoiceListView>();

            // Register customer views
            services.AddTransient<CustomerView>();
            services.AddTransient<CustomerDialogView>();
            services.AddTransient<SettleDebtDialogView>();

            // Register transaction views
            services.AddTransient<TransactionHistoryView>();
            services.AddTransient<TransactionDetailsDialogView>();

            // Register login window
            services.AddTransient<LoginView>();

            // Register authentication viewmodel
            services.AddTransient(provider =>
                new LoginViewModel(
                    provider.GetRequiredService<IAuthService>(),
                    user =>
                    {
                        _currentUser = user;
                        ShowMainWindow();
                    }));
        }

        /// <summary>
        /// Configures XAML diagnostic tracing
        /// </summary>
        private void ConfigureXamlDiagnostics()
        {
            // Enable XAML diagnostic tracing
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(
                new XamlTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }

        /// <summary>
        /// Ensures all required resources are loaded
        /// </summary>
        private void EnsureResourcesLoaded()
        {
            try
            {
                // Try to load the common resources
                var resourceUri = new Uri("pack://application:,,,/OfflinePOS.Core;component/Styles/CommonStyles.xaml", UriKind.Absolute);
                var resourceDict = new ResourceDictionary { Source = resourceUri };

                // Add to the application resources if loaded successfully
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);

                // Load converter resources
                var converterResourceUri = new Uri("pack://application:,,,/OfflinePOS.Core;component/Converters/ConverterResources.xaml", UriKind.Absolute);
                var converterResourceDict = new ResourceDictionary { Source = converterResourceUri };
                Application.Current.Resources.MergedDictionaries.Add(converterResourceDict);

                _logger?.LogInformation("Common resources loaded successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load common resources. UI might be affected.");
                // Continue application execution despite resource loading issues
            }
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

                // Create the main window
                var mainWindow = new MainWindow(_currentUser, _serviceProvider);

                // CRITICAL: Set as application's main window
                Application.Current.MainWindow = mainWindow;

                // Add a strong reference to prevent GC and inadvertent collection
                Current.Properties["MainWindow"] = mainWindow;

                // Show the window
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