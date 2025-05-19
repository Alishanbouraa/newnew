// OfflinePOS.Admin/Views/CustomerDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    public partial class CustomerDialogView : Window
    {
        private readonly CustomerDialogViewModel _viewModel;

        public CustomerDialogView(CustomerDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to close request event
            _viewModel.CloseRequested += ViewModel_CloseRequested;
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