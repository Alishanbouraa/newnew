// OfflinePOS.Admin/Views/StockManagementView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for StockManagementView.xaml
    /// </summary>
    public partial class StockManagementView : UserControl
    {
        private readonly StockManagementViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the StockManagementView class
        /// </summary>
        public StockManagementView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the StockManagementView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel for the view</param>
        public StockManagementView(StockManagementViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void StockManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    // Load products when the view is loaded
                    await _viewModel.LoadProductsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock management view: {ex.Message}",
                       "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}