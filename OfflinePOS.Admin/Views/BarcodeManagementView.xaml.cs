// OfflinePOS.Admin/Views/BarcodeManagementView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for BarcodeManagementView.xaml
    /// </summary>
    public partial class BarcodeManagementView : UserControl
    {
        private readonly BarcodeManagementViewModel _viewModel;

        // Constructor implementations...

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        private async void BarcodeManagementView_Loaded(object sender, RoutedEventArgs e)
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
                MessageBox.Show($"Error loading barcode management view: {ex.Message}",
                       "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}