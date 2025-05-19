// OfflinePOS.Admin/Views/SupplierInvoiceListView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for SupplierInvoiceListView.xaml
    /// </summary>
    public partial class SupplierInvoiceListView : Window
    {
        private readonly SupplierInvoiceListViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceListView class
        /// </summary>
        public SupplierInvoiceListView(SupplierInvoiceListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to close request event
            _viewModel.CloseRequested += ViewModel_CloseRequested;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load invoices when window is shown
                await _viewModel.LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoices: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_CloseRequested(object sender, bool result)
        {
            // Unsubscribe to avoid memory leaks
            _viewModel.CloseRequested -= ViewModel_CloseRequested;

            // Close the window
            Close();
        }
    }
}