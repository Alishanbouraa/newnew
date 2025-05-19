// OfflinePOS.Admin/Views/TransactionDetailsDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    public partial class TransactionDetailsDialogView : Window
    {
        private readonly TransactionDetailsViewModel _viewModel;

        public TransactionDetailsDialogView(TransactionDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }
    }
}