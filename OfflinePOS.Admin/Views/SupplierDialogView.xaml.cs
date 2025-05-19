// OfflinePOS.Admin/Views/SupplierDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    public partial class SupplierDialogView : Window
    {
        private readonly SupplierDialogViewModel _viewModel;

        public SupplierDialogView(SupplierDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to close request event
            _viewModel.CloseRequested += ViewModel_CloseRequested;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load data when the window is displayed
            _viewModel.LoadPaymentTerms();
        }

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