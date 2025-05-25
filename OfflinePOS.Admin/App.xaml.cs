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

        /// <summary>
        /// Configures application services for dependency injection with proper DbContext scoping
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

            // FIXED: Register DbContext with scoped lifetime to prevent concurrency issues
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    _configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    }),
                    ServiceLifetime.Scoped);

            // Register database initializer
            services.AddTransient<DatabaseInitializer>();

            // FIXED: Register repositories and UoW with scoped lifetime to match DbContext
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // FIXED: Register core services as scoped to prevent DbContext sharing
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IDrawerService, DrawerService>();

            // Register SupplierInvoiceDetailsViewModel factory
            services.AddTransient<Func<SupplierInvoice, Supplier, SupplierInvoiceDetailsViewModel>>(
                provider => (invoice, supplier) =>
                    new SupplierInvoiceDetailsViewModel(
                        provider.GetRequiredService<ISupplierInvoiceService>(),
                        provider.GetRequiredService<IProductService>(),
                        provider.GetRequiredService<ILogger<SupplierInvoiceDetailsViewModel>>(),
                        _currentUser,
                        invoice,
                        supplier,
                        provider));

            // Register views
            services.AddTransient<SupplierInvoiceDetailsView>();

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

            // Register Supplier Invoice Dialog factory
            services.AddTransient<Func<Supplier, SupplierInvoiceDialogViewModel>>(provider => (supplier) =>
                new SupplierInvoiceDialogViewModel(
                    provider.GetRequiredService<ISupplierInvoiceService>(),
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<SupplierInvoiceDialogViewModel>>(),
                    _currentUser,
                    supplier));

            services.AddTransient<SupplierInvoiceDialogView>();

            // Register Product Import/Export ViewModel
            services.AddTransient(provider =>
                new ProductImportExportViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<IStockService>(),
                    provider.GetRequiredService<ILogger<ProductImportExportViewModel>>(),
                    _currentUser));

            // FIXED: Register ProductViewModel with service provider for proper scoping
            services.AddTransient(provider =>
                new ProductViewModel(
                    provider.GetRequiredService<IProductService>(),
                    provider.GetRequiredService<IStockService>(),
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<ProductViewModel>>(),
                    _currentUser,
                    provider));

            // Register Category ViewModels
            services.AddTransient(provider =>
                new CategoryViewModel(
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ILogger<CategoryViewModel>>(),
                    _currentUser,
                    provider));

            // Register Supplier ViewModels
            services.AddTransient(provider =>
                new SupplierViewModel(
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<SupplierViewModel>>(),
                    _currentUser,
                    provider));

            // Register Supplier Invoice List factory
            services.AddTransient<Func<Supplier, SupplierInvoiceListViewModel>>(provider => (supplier) =>
                new SupplierInvoiceListViewModel(
                    provider.GetRequiredService<ISupplierInvoiceService>(),
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<IProductService>(),
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

            // FIXED: Register ViewModel factories with service provider pattern
            services.AddTransient<Func<Category, bool, CategoryDialogViewModel>>(provider => (category, isNew) =>
                new CategoryDialogViewModel(
                    provider.GetRequiredService<ICategoryService>(),
                    provider.GetRequiredService<ILogger<CategoryDialogViewModel>>(),
                    _currentUser,
                    category,
                    isNew));

            // FIXED: Updated ProductDialogViewModel factory to use service provider
            services.AddTransient<Func<Product, ProductDialogViewModel>>(provider => (product) =>
                new ProductDialogViewModel(
                    provider,
                    provider.GetRequiredService<ILogger<ProductDialogViewModel>>(),
                    _currentUser,
                    product));

            // Register Supplier Dialog factory
            services.AddTransient<Func<Supplier, SupplierDialogViewModel>>(provider => (supplier) =>
                new SupplierDialogViewModel(
                    provider.GetRequiredService<ISupplierService>(),
                    provider.GetRequiredService<ILogger<SupplierDialogViewModel>>(),
                    _currentUser,
                    supplier));

            // Register Customer Dialog factory
            services.AddTransient<Func<Customer, CustomerDialogViewModel>>(provider => (customer) =>
                new CustomerDialogViewModel(
                    provider.GetRequiredService<ICustomerService>(),
                    provider.GetRequiredService<ILogger<CustomerDialogViewModel>>(),
                    _currentUser,
                    customer));

            // Register Settle Debt factory
            services.AddTransient<Func<Customer, SettleDebtViewModel>>(provider => (customer) =>
                new SettleDebtViewModel(
                    provider.GetRequiredService<ICustomerService>(),
                    provider.GetRequiredService<ILogger<SettleDebtViewModel>>(),
                    _currentUser,
                    customer));

            // Register Transaction Details factory
            services.AddTransient<Func<Transaction, TransactionDetailsViewModel>>(provider => (transaction) =>
                new TransactionDetailsViewModel(
                    provider.GetRequiredService<ITransactionService>(),
                    provider.GetRequiredService<ILogger<TransactionDetailsViewModel>>(),
                    transaction));

            // Register Supplier Payment factory
            services.AddTransient<Func<Supplier, SupplierInvoice, SupplierPaymentViewModel>>(provider => (supplier, invoice) =>
                new SupplierPaymentViewModel(
                    provider.GetRequiredService<ISupplierInvoiceService>(),
                    provider.GetRequiredService<ILogger<SupplierPaymentViewModel>>(),
                    _currentUser,
                    supplier,
                    invoice));

            // Register all views
            RegisterViews(services);

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
        /// Registers all view components
        /// </summary>
        private void RegisterViews(IServiceCollection services)
        {
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
            services.AddTransient<SupplierPaymentDialogView>();

            // Register customer views
            services.AddTransient<CustomerView>();
            services.AddTransient<CustomerDialogView>();
            services.AddTransient<SettleDebtDialogView>();

            // Register transaction views
            services.AddTransient<TransactionHistoryView>();
            services.AddTransient<TransactionDetailsDialogView>();

            // Register login window
            services.AddTransient<LoginView>();
        }

        /// <summary>
        /// Configures XAML diagnostic tracing for debugging
        /// </summary>
        private void ConfigureXamlDiagnostics()
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(
                new XamlTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }

        /// <summary>
        /// Ensures all required WPF resources are loaded properly
        /// </summary>
        private void EnsureResourcesLoaded()
        {
            try
            {
                var resourceUri = new Uri("pack://application:,,,/OfflinePOS.Core;component/Styles/CommonStyles.xaml", UriKind.Absolute);
                var resourceDict = new ResourceDictionary { Source = resourceUri };
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);

                var converterResourceUri = new Uri("pack://application:,,,/OfflinePOS.Core;component/Converters/ConverterResources.xaml", UriKind.Absolute);
                var converterResourceDict = new ResourceDictionary { Source = converterResourceUri };
                Application.Current.Resources.MergedDictionaries.Add(converterResourceDict);

                _logger?.LogInformation("Common resources loaded successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load common resources. UI might be affected.");
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
        /// Shows the main application window after successful authentication
        /// </summary>
        private void ShowMainWindow()
        {
            try
            {
                // Close all existing windows
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

                _logger.LogInformation($"Creating main window for user: {_currentUser.Username}");

                // Create and configure main window
                var mainWindow = new MainWindow(_currentUser, _serviceProvider);
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
    }
}