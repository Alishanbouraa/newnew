// OfflinePOS.Admin/Views/SupplierPaymentDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for SupplierPaymentDialogView.xaml
    /// </summary>
    public partial class SupplierPaymentDialogView : Window
    {
        private readonly SupplierPaymentViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the SupplierPaymentDialogView class
        /// </summary>
        /// <param name="viewModel">The view model</param>
        public SupplierPaymentDialogView(SupplierPaymentViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to close request event
            _viewModel.CloseRequested += ViewModel_CloseRequested;
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