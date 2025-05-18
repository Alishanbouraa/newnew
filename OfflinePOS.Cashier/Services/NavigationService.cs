// OfflinePOS.Cashier/Services/NavigationService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Cashier.ViewModels;
using OfflinePOS.Cashier.Views;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Services;
using System;
using System.Windows;
using System.Windows.Threading;

namespace OfflinePOS.Cashier.Services
{
    /// <summary>
    /// Implementation of the navigation service for the cashier application
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly ILogger<NavigationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private MainWindow _mainWindow;
        private User _currentUser;

        /// <summary>
        /// Initializes a new instance of the NavigationService class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="serviceProvider">Service provider</param>
        public NavigationService(
            ILogger<NavigationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Initializes the navigation service with the main window and current user
        /// </summary>
        /// <param name="mainWindow">Main application window</param>
        /// <param name="currentUser">Current authenticated user</param>
        public void Initialize(MainWindow mainWindow, User currentUser)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger.LogInformation("Navigation service initialized with MainWindow and User");
        }

        /// <summary>
        /// Navigates to the specified view
        /// </summary>
        /// <param name="viewName">Name of the view to navigate to</param>
        public void NavigateTo(string viewName)
        {
            if (_mainWindow == null)
            {
                _logger.LogError("Cannot navigate: MainWindow not initialized");
                return;
            }

            // Ensure UI operations run on the UI thread
            if (!_mainWindow.Dispatcher.CheckAccess())
            {
                _mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => NavigateTo(viewName)));
                return;
            }

            try
            {
                _logger.LogInformation($"Navigating to view: {viewName}");

                switch (viewName)
                {
                    case "SalesView":
                        var salesViewModel = _serviceProvider.GetRequiredService<SalesViewModel>();
                        var salesView = new SalesView(salesViewModel);
                        _mainWindow.MainContent.Content = salesView;
                        _mainWindow.StatusText.Text = "View: SalesView";

                        // Initialize the view model asynchronously
                        salesViewModel.InitializeAsync().ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _mainWindow.Dispatcher.Invoke(() =>
                                {
                                    _logger.LogError(t.Exception, "Error initializing SalesViewModel");
                                    _mainWindow.StatusText.Text = "Error initializing SalesView";
                                });
                            }
                        }, TaskScheduler.Current);

                        _logger.LogInformation("Successfully navigated to SalesView");
                        break;

                    case "DrawerView":
                        var drawerViewModel = _serviceProvider.GetRequiredService<DrawerViewModel>();
                        var drawerView = new DrawerView(drawerViewModel);
                        _mainWindow.MainContent.Content = drawerView;
                        _mainWindow.StatusText.Text = "View: DrawerView";

                        // Initialize the view model asynchronously
                        drawerViewModel.InitializeAsync().ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _mainWindow.Dispatcher.Invoke(() =>
                                {
                                    _logger.LogError(t.Exception, "Error initializing DrawerViewModel");
                                    _mainWindow.StatusText.Text = "Error initializing DrawerView";
                                });
                            }
                        }, TaskScheduler.Current);

                        _logger.LogInformation("Successfully navigated to DrawerView");
                        break;

                    case "Logout":
                        Logout();
                        break;

                    default:
                        _logger.LogWarning($"Unknown view name: {viewName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to view: {viewName}");

                if (_mainWindow != null)
                {
                    _mainWindow.StatusText.Text = $"Navigation error: {ex.Message}";
                    MessageBox.Show($"Error navigating to {viewName}: {ex.Message}",
                                   "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Logs out of the application
        /// </summary>
        public void Logout()
        {
            try
            {
                _logger.LogInformation("Logging out user");

                // Show login window
                var loginView = _serviceProvider.GetRequiredService<LoginView>();
                loginView.Show();

                // Close main window
                if (_mainWindow != null)
                {
                    _mainWindow.Close();
                    _mainWindow = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                MessageBox.Show("Error logging out. Please try again.",
                                "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}