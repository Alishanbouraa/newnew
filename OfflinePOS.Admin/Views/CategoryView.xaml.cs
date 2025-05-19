// File: OfflinePOS.Admin/Views/CategoryView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for CategoryView.xaml
    /// </summary>
    public partial class CategoryView : UserControl
    {
        private readonly CategoryViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the CategoryView class
        /// </summary>
        /// <param name="viewModel">Category view model</param>
        public CategoryView(CategoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Load data when view is loaded
            Loaded += async (s, e) => await _viewModel.LoadDataAsync();
        }
    }
}