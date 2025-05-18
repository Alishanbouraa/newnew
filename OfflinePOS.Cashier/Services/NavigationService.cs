// File: OfflinePOS.Cashier/Services/NavigationService.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Services;
using System;
using System.Windows;

namespace OfflinePOS.Cashier.Services
{
    /// <summary>
    /// Implementation of the navigation service for the cashier application
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly ILogger<NavigationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MainWindow _mainWindow;

        /// <summary>
        /// Initializes a new instance of the NavigationService class
        /// </summary>
        public NavigationService(
            ILogger<NavigationService> logger,
            IServiceProvider serviceProvider,
            MainWindow mainWindow)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        /// <summary>
        /// Navigates to the specified view
        /// </summary>
        public void NavigateTo(string viewName)
        {
            try
            {
                _logger.LogInformation($"Navigating to view: {viewName}");

                switch (viewName)
                {
                    case "SalesView":
                        _mainWindow.NavigateToView<Views.SalesView, ViewModels.SalesViewModel>();
                        break;
                    case "DrawerView":
                        _mainWindow.NavigateToView<Views.DrawerView, ViewModels.DrawerViewModel>();
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
                var loginView = _serviceProvider.GetRequiredService<Views.LoginView>();
                loginView.Show();

                // Close main window
                _mainWindow.Close();
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