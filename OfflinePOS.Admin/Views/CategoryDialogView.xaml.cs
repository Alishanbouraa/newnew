// File: OfflinePOS.Admin/Views/CategoryDialogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for CategoryDialogView.xaml
    /// </summary>
    public partial class CategoryDialogView : Window
    {
        private readonly CategoryDialogViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the CategoryDialogView class
        /// </summary>
        /// <param name="viewModel">Category dialog view model</param>
        public CategoryDialogView(CategoryDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Handle close request from view model
            _viewModel.CloseRequested += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}