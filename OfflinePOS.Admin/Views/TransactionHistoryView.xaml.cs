// OfflinePOS.Admin/Views/TransactionHistoryView.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    public partial class TransactionHistoryView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private TransactionHistoryViewModel _viewModel;

        public TransactionHistoryView()
        {
            InitializeComponent();
            _serviceProvider = ((App)Application.Current).ServiceProvider;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the view model from DI
                _viewModel = _serviceProvider.GetRequiredService<TransactionHistoryViewModel>();

                // Set as DataContext
                DataContext = _viewModel;

                // Load initial data
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}