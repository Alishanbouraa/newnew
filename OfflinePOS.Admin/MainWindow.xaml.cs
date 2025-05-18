// OfflinePOS.Admin/MainWindow.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin
{
    public partial class MainWindow : Window
    {
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(User currentUser, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _serviceProvider = serviceProvider;

            // Set user info
            UserNameTextBlock.Text = _currentUser.FullName;

            // Select first item in navigation
            if (NavigationListBox.Items.Count > 0)
            {
                NavigationListBox.SelectedIndex = 0;
            }
        }

        private void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavigationListBox.SelectedItem is ListBoxItem selectedItem)
            {
                // Update page title
                PageTitleTextBlock.Text = selectedItem.Content.ToString();

                // Here you would navigate to different views based on selection
                // This will be implemented in future steps
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Show login window again using the service provider
            var loginView = _serviceProvider.GetRequiredService<LoginView>();
            loginView.Show();

            // Close this window
            Close();
        }
    }
}