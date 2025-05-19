// OfflinePOS.Admin/Views/SupplierInvoiceDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for SupplierInvoiceDialogView.xaml
    /// </summary>
    public partial class SupplierInvoiceDialogView : Window
    {
        private readonly SupplierInvoiceDialogViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceDialogView class
        /// </summary>
        /// <param name="viewModel">The view model</param>
        public SupplierInvoiceDialogView(SupplierInvoiceDialogViewModel viewModel)
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
                MessageBox.Show($"Error loading data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the product search text box key down event
        /// </summary>
        private void ProductSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null)
            {
                _viewModel.SearchProductsCommand.Execute(null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the close requested event from the view model
        /// </summary>
        private void ViewModel_CloseRequested(object sender, bool result)
        {
            // Unsubscribe to avoid memory leaks
            _viewModel.CloseRequested -= ViewModel_CloseRequested;

            // Set dialog result and close
            DialogResult = result;
            Close();
        }
    }
}