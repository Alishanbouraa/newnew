// OfflinePOS.Admin/MainWindow.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin
{
    public partial class MainWindow : Window
    {
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(User currentUser, IServiceProvider serviceProvider)
        {
            try
            {
                _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _logger = _serviceProvider.GetService<ILogger<MainWindow>>();

                InitializeComponent();

                // Set user info
                UserNameTextBlock.Text = _currentUser.FullName;

                // Select first item in navigation
                if (NavigationListBox.Items.Count > 0)
                {
                    NavigationListBox.SelectedIndex = 0;
                }

                _logger?.LogDebug($"MainWindow initialized successfully for user: {_currentUser.Username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing MainWindow");
                MessageBox.Show($"Error initializing main window: {ex.Message}",
                               "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Rethrow to be caught by the exception handler in App.xaml.cs
            }
        }

        private void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (NavigationListBox.SelectedItem is ListBoxItem selectedItem)
                {
                    // Update page title
                    PageTitleTextBlock.Text = selectedItem.Content.ToString();

                    // Here you would navigate to different views based on selection
                    // This will be implemented in future steps
                    _logger?.LogDebug($"Navigation changed to: {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during navigation change");
            }
        }

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