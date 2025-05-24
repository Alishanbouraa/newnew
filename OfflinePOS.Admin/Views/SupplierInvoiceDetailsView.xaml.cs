// OfflinePOS.Admin/Views/SupplierInvoiceDetailsView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for SupplierInvoiceDetailsView.xaml
    /// </summary>
    public partial class SupplierInvoiceDetailsView : Window
    {
        private readonly SupplierInvoiceDetailsViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceDetailsView class
        /// </summary>
        /// <param name="viewModel">The view model</param>
        public SupplierInvoiceDetailsView(SupplierInvoiceDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to close request event
            _viewModel.CloseRequested += ViewModel_CloseRequested;
        }

        /// <summary>
        /// Handles the window loaded event
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load data when window is shown
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice details: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the close requested event from the view model
        /// </summary>
        private void ViewModel_CloseRequested(object sender, bool result)
        {
            // Unsubscribe to avoid memory leaks
            _viewModel.CloseRequested -= ViewModel_CloseRequested;

            // Close the window
            Close();
        }
    }
}