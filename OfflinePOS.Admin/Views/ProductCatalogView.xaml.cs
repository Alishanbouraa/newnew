// OfflinePOS.Admin/Views/ProductCatalogView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for ProductCatalogView.xaml
    /// </summary>
    public partial class ProductCatalogView : UserControl
    {
        private readonly ProductCatalogViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the ProductCatalogView class
        /// </summary>
        public ProductCatalogView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the ProductCatalogView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel for the view</param>
        public ProductCatalogView(ProductCatalogViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        private async void ProductCatalogView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading product catalog view: {ex.Message}",
                       "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}