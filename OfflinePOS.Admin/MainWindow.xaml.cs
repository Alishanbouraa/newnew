// File: OfflinePOS.Admin/MainWindow.xaml.cs - Enhanced with Inventory Management Navigation
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Diagnostics;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin
{
    /// <summary>
    /// Enhanced interaction logic for MainWindow.xaml with comprehensive inventory management
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
                _logger?.LogDebug("Enhanced MainWindow constructor starting for inventory management");

                // Initialize component first
                InitializeComponent();

                _logger?.LogDebug("InitializeComponent completed for enhanced window");

                // Mark as initialized so closing event doesn't prematurely terminate
                _isInitialized = true;

                _logger?.LogInformation($"Enhanced MainWindow constructor completed for user: {_currentUser.Username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing enhanced MainWindow");
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
                _logger?.LogInformation($"Enhanced MainWindow.Loaded event for user: {_currentUser.Username}");

                // Verify XAML elements for debugging purposes
                MainWindowDiagnostics.VerifyXamlElements(this, _logger);

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

                // Select first item in navigation safely (Dashboard)
                if (NavigationListBox != null && NavigationListBox.Items.Count > 0)
                {
                    NavigationListBox.SelectedIndex = 0;
                    _logger?.LogDebug("NavigationListBox selection set successfully to Dashboard");
                }
                else
                {
                    _logger?.LogWarning($"NavigationListBox is null or empty: {NavigationListBox?.Items?.Count ?? -1} items");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in enhanced MainWindow_Loaded event");
                MessageBox.Show($"Error loading main window: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the Window.ContentRendered event
        /// </summary>
        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            _logger?.LogInformation("Enhanced MainWindow.ContentRendered event fired");
        }

        /// <summary>
        /// Handles the Window.Closing event
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Only log if properly initialized
            if (_isInitialized)
            {
                _logger?.LogInformation($"Enhanced MainWindow closing for user: {_currentUser.Username}");

                if (Application.Current.MainWindow == this)
                {
                    _logger?.LogInformation("Application shutting down from enhanced MainWindow closing");
                    Application.Current.Shutdown();
                }
            }
            else
            {
                // If we're closing during initialization, there may be an error
                _logger?.LogWarning("Enhanced MainWindow closing during initialization - possible error");
            }
        }

        /// <summary>
        /// Handles the enhanced navigation list box selection change with inventory management support
        /// </summary>
        private void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (NavigationListBox?.SelectedItem is ListBoxItem selectedItem && PageTitleTextBlock != null)
                {
                    // Extract view name from the ListBoxItem content
                    string viewName = ExtractViewName(selectedItem);
                    PageTitleTextBlock.Text = viewName;
                    _logger?.LogDebug($"Enhanced navigation changed to: {viewName}");

                    // Navigate to the appropriate view based on selection
                    switch (viewName)
                    {
                        case "Dashboard":
                            LoadDashboardView();
                            break;

                        // NEW: Enhanced Inventory Management
                        case "Inventory Management":
                            LoadView<InventoryManagementView>("Inventory Management - Main Stock Storage");
                            break;

                        case "Product Catalog":
                            LoadView<ProductCatalogView>("Product Catalog - Products Available for Sale");
                            break;

                        case "Stock Management":
                            LoadView<StockManagementView>("Stock Management - Stock Control & Adjustments");
                            break;

                        // Enhanced Product Management
                        case "Products":
                            LoadView<ProductView>("Products - Manage Product Information");
                            break;

                        case "Categories":
                            LoadView<CategoryView>("Categories - Organize Products");
                            break;

                        // Business Management
                        case "Suppliers":
                            LoadView<SupplierView>("Suppliers - Vendor Management");
                            break;

                        case "Customers":
                            LoadView<CustomerView>("Customers - Customer Management");
                            break;

                        case "Transactions":
                            LoadView<TransactionHistoryView>("Transactions - Sales History");
                            break;

                        // System Management
                        case "Employees":
                            LoadPlaceholderView("👤 Employees", "Employee management functionality will be implemented soon.");
                            break;

                        case "Reports":
                            LoadPlaceholderView("📈 Reports", "Advanced reporting features will be implemented soon.");
                            break;

                        case "Settings":
                            LoadPlaceholderView("⚙️ Settings", "System settings will be implemented soon.");
                            break;

                        default:
                            LoadPlaceholderView("🔧 Coming Soon", $"View for {viewName} not implemented yet.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during enhanced navigation change");
                MessageBox.Show($"Error navigating to selected item: {ex.Message}",
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Extracts the view name from a ListBoxItem that may contain StackPanel with icon and text
        /// </summary>
        /// <param name="item">ListBoxItem to extract name from</param>
        /// <returns>View name string</returns>
        private string ExtractViewName(ListBoxItem item)
        {
            if (item.Content is StackPanel stackPanel)
            {
                // Find the TextBlock that contains the actual text (not the icon)
                foreach (var child in stackPanel.Children)
                {
                    if (child is TextBlock textBlock && textBlock.Text.Length > 2) // Exclude icon TextBlocks
                    {
                        return textBlock.Text;
                    }
                }
            }
            else if (item.Content is string content)
            {
                return content;
            }

            return item.Content?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Loads the enhanced dashboard view with inventory management highlights
        /// </summary>
        private void LoadDashboardView()
        {
            try
            {
                // Create enhanced dashboard content
                var dashboardGrid = new Grid();
                dashboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var welcomePanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Dashboard header
                var headerText = new TextBlock
                {
                    Text = "🎯 Enhanced POS Dashboard",
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.DarkSlateGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var subtitleText = new TextBlock
                {
                    Text = "Complete Inventory Management System",
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 30)
                };

                // Quick action buttons
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var inventoryButton = CreateQuickActionButton("📦 Manage Inventory", "Inventory Management");
                var catalogButton = CreateQuickActionButton("🛍️ View Catalog", "Product Catalog");
                var stockButton = CreateQuickActionButton("📋 Check Stock", "Stock Management");

                buttonPanel.Children.Add(inventoryButton);
                buttonPanel.Children.Add(catalogButton);
                buttonPanel.Children.Add(stockButton);

                welcomePanel.Children.Add(headerText);
                welcomePanel.Children.Add(subtitleText);
                welcomePanel.Children.Add(buttonPanel);

                dashboardGrid.Children.Add(welcomePanel);
                MainContent.Content = dashboardGrid;

                _logger?.LogInformation("Enhanced dashboard view loaded with inventory management features");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading enhanced dashboard view");
                LoadPlaceholderView("❌ Error", "Error loading dashboard view");
            }
        }

        /// <summary>
        /// Creates a quick action button for dashboard navigation
        /// </summary>
        /// <param name="content">Button text content</param>
        /// <param name="targetView">Target view name</param>
        /// <returns>Configured button</returns>
        private Button CreateQuickActionButton(string content, string targetView)
        {
            var button = new Button
            {
                Content = content,
                Padding = new Thickness(15, 10),
                Margin = new Thickness(5),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = System.Windows.Media.Brushes.SteelBlue,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Tag = targetView
            };

            button.Click += QuickAction_Click;
            return button;
        }

        /// <summary>
        /// Handles quick action button clicks from dashboard
        /// </summary>
        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string targetView)
                {
                    // Find and select the corresponding navigation item
                    for (int i = 0; i < NavigationListBox.Items.Count; i++)
                    {
                        if (NavigationListBox.Items[i] is ListBoxItem item)
                        {
                            string itemName = ExtractViewName(item);
                            if (itemName == targetView)
                            {
                                NavigationListBox.SelectedIndex = i;
                                _logger?.LogDebug($"Quick action navigated to: {targetView}");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling quick action click");
            }
        }

        /// <summary>
        /// Loads a view from the DI container and sets it as the main content
        /// </summary>
        /// <typeparam name="T">Type of view to load</typeparam>
        /// <param name="pageTitle">Optional page title override</param>
        private void LoadView<T>(string pageTitle = null) where T : UserControl
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

                    // Update page title if provided
                    if (!string.IsNullOrEmpty(pageTitle) && PageTitleTextBlock != null)
                    {
                        PageTitleTextBlock.Text = pageTitle;
                    }

                    _logger?.LogInformation($"Enhanced view loaded: {typeof(T).Name}");
                }
                else
                {
                    _logger?.LogWarning($"Enhanced view {typeof(T).Name} could not be resolved from DI container");
                    LoadPlaceholderView("❌ Error", $"Could not load {typeof(T).Name}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error loading enhanced view {typeof(T).Name}");
                LoadPlaceholderView("❌ Error", $"Error loading view: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a placeholder view for unimplemented features
        /// </summary>
        /// <param name="title">Placeholder title</param>
        /// <param name="message">Placeholder message</param>
        private void LoadPlaceholderView(string title, string message)
        {
            try
            {
                var placeholderGrid = new Grid();
                placeholderGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var placeholderPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var titleText = new TextBlock
                {
                    Text = title,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var messageText = new TextBlock
                {
                    Text = message,
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.DarkGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400
                };

                placeholderPanel.Children.Add(titleText);
                placeholderPanel.Children.Add(messageText);

                placeholderGrid.Children.Add(placeholderPanel);
                MainContent.Content = placeholderGrid;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading placeholder view");
                MainContent.Content = new TextBlock
                {
                    Text = "Error loading content",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        /// <summary>
        /// Handles the logout button click
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation($"User {_currentUser.Username} logging out from enhanced system");

                // Show login window again using the service provider
                var loginView = _serviceProvider.GetRequiredService<LoginView>();
                loginView.Show();

                // Close this window
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during logout from enhanced system");
                MessageBox.Show($"Error during logout: {ex.Message}",
                               "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}