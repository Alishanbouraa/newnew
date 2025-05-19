// OfflinePOS.Admin/Views/CustomerView.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OfflinePOS.Admin.Views
{
    public partial class CustomerView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private CustomerViewModel _viewModel;

        public CustomerView()
        {
            InitializeComponent();
            _serviceProvider = ((App)Application.Current).ServiceProvider;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the view model from DI
                _viewModel = _serviceProvider.GetRequiredService<CustomerViewModel>();

                // Set as DataContext
                DataContext = _viewModel;

                // Load initial data
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null)
            {
                _viewModel.SearchCustomersCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}