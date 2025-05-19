// OfflinePOS.Admin/Views/SettleDebtDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    public partial class SettleDebtDialogView : Window
    {
        private readonly SettleDebtViewModel _viewModel;

        public SettleDebtDialogView(SettleDebtViewModel viewModel)
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