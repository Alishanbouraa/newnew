using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml - Main administrative interface
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;
        private bool _isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        /// <param name="currentUser">The authenticated user</param>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        public MainWindow(User currentUser, IServiceProvider serviceProvider)
        {
            try
            {
                _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _logger = _serviceProvider.GetService<ILogger<MainWindow>>();

                // Log before initialization
                _logger?.LogDebug("MainWindow constructor starting");

                // Initialize component first
                InitializeComponent();

                _logger?.LogDebug("InitializeComponent completed");

                // Mark as initialized so closing event doesn't prematurely terminate
                _isInitialized = true;

                _logger?.LogInformation($"MainWindow constructor completed for user: {_currentUser.Username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing MainWindow");
                MessageBox.Show($"Error initializing main window: {ex.Message}",
                               "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Handles the Window.Loaded event
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation($"MainWindow.Loaded event for user: {_currentUser.Username}");

                // Set user info safely
                if (UserNameTextBlock != null)
                {
                    UserNameTextBlock.Text = _currentUser.FullName;
                    _logger?.LogDebug("UserNameTextBlock.Text set successfully");
                }
                else
                {
                    _logger?.LogWarning("UserNameTextBlock is null!");
                }

                // Select first item in navigation safely
                if (NavigationListBox != null && NavigationListBox.Items.Count > 0)
                {
                    NavigationListBox.SelectedIndex = 0;
                    _logger?.LogDebug("NavigationListBox selection set successfully");
                }
                else
                {
                    _logger?.LogWarning($"NavigationListBox is null or empty: {NavigationListBox?.Items?.Count ?? -1} items");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in MainWindow_Loaded event");
            }
        }

        /// <summary>
        /// Handles the Window.ContentRendered event
        /// </summary>
        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            _logger?.LogInformation("MainWindow.ContentRendered event fired");
        }

        /// <summary>
        /// Handles the Window.Closing event
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Only log if properly initialized
            if (_isInitialized)
            {
                _logger?.LogInformation($"MainWindow closing for user: {_currentUser.Username}");

                if (Application.Current.MainWindow == this)
                {
                    _logger?.LogInformation("Application shutting down from MainWindow closing");
                }
            }
            else
            {
                // If we're closing during initialization, there may be an error
                _logger?.LogWarning("MainWindow closing during initialization - possible error");
            }
        }

        /// <summary>
        /// Handles the navigation list box selection change
        /// </summary>
        private void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (NavigationListBox?.SelectedItem is ListBoxItem selectedItem && PageTitleTextBlock != null)
                {
                    // Update page title
                    string viewName = selectedItem.Content.ToString();
                    PageTitleTextBlock.Text = viewName;
                    _logger?.LogDebug($"Navigation changed to: {viewName}");

                    // Navigate to the appropriate view based on selection
                    switch (viewName)
                    {
                        case "Dashboard":
                            MainContent.Content = "Welcome to POS Admin System";
                            break;
                        case "Products":
                            LoadView<ProductView>();
                            break;
                        case "Categories":
                            // Load CategoryView
                            MainContent.Content = "Categories view not implemented yet";
                            break;
                        case "Suppliers":
                            // Load SupplierView
                            MainContent.Content = "Suppliers view not implemented yet";
                            break;
                        case "Customers":
                            // Load CustomerView
                            MainContent.Content = "Customers view not implemented yet";
                            break;
                        case "Transactions":
                            // Load TransactionView
                            MainContent.Content = "Transactions view not implemented yet";
                            break;
                        case "Employees":
                            // Load EmployeeView
                            MainContent.Content = "Employees view not implemented yet";
                            break;
                        case "Reports":
                            // Load ReportView
                            MainContent.Content = "Reports view not implemented yet";
                            break;
                        case "Settings":
                            // Load SettingsView
                            MainContent.Content = "Settings view not implemented yet";
                            break;
                        default:
                            MainContent.Content = $"View for {viewName} not implemented yet.";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during navigation change");
                MessageBox.Show($"Error navigating to selected item: {ex.Message}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Loads a view from the DI container and sets it as the main content
        /// </summary>
        /// <typeparam name="T">Type of view to load</typeparam>
        private void LoadView<T>() where T : UserControl
        {
            try
            {
                // Request the view from the DI container
                var view = _serviceProvider.GetService<T>();
                if (view != null)
                {
                    // Clear any existing content first to ensure proper initialization
                    MainContent.Content = null;

                    // Set the new content
                    MainContent.Content = view;
                    _logger?.LogInformation($"Loaded view: {typeof(T).Name}");
                }
                else
                {
                    _logger?.LogWarning($"View {typeof(T).Name} could not be resolved from DI container");
                    MainContent.Content = $"Error: Could not load {typeof(T).Name}";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error loading view {typeof(T).Name}");
                MainContent.Content = $"Error loading view: {ex.Message}";
            }
        }

        /// <summary>
        /// Handles the logout button click
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation($"User {_currentUser.Username} logging out");

                // Show login window again using the service provider
                var loginView = _serviceProvider.GetRequiredService<LoginView>();
                loginView.Show();

                // Close this window
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during logout");
                MessageBox.Show($"Error during logout: {ex.Message}",
                               "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}