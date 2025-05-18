// OfflinePOS.Admin/Views/ProductImportExportView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for ProductImportExportView.xaml
    /// </summary>
    public partial class ProductImportExportView : UserControl
    {
        private readonly ProductImportExportViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the ProductImportExportView class
        /// </summary>
        public ProductImportExportView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the ProductImportExportView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel for the view</param>
        public ProductImportExportView(ProductImportExportViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void ProductImportExportView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set default status message
                if (_viewModel != null)
                {
                    _viewModel.StatusMessage = "Ready to import or export products. Choose an option from the tabs above.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading import/export view: {ex.Message}",
                       "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}