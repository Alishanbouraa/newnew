// OfflinePOS.Admin/Views/SupplierView.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OfflinePOS.Admin.Views
{
    public partial class SupplierView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private SupplierViewModel _viewModel;

        public SupplierView()
        {
            InitializeComponent();
            _serviceProvider = ((App)Application.Current).ServiceProvider;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the view model from DI
                _viewModel = _serviceProvider.GetRequiredService<SupplierViewModel>();

                // Set as DataContext
                DataContext = _viewModel;

                // Load initial data
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null)
            {
                _viewModel.SearchSuppliersCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}