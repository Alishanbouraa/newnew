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

        // Constructor implementations...

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        private async void StockManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    // Call the public method instead of the protected one
                    await _viewModel.LoadProductsPublicAsync();
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