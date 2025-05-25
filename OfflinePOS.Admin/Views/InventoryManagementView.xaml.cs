// OfflinePOS.Admin/Views/InventoryManagementView.xaml.cs
using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for InventoryManagementView.xaml
    /// </summary>
    public partial class InventoryManagementView : UserControl
    {
        private readonly InventoryManagementViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the InventoryManagementView class
        /// </summary>
        public InventoryManagementView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the InventoryManagementView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel for the view</param>
        public InventoryManagementView(InventoryManagementViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        private async void InventoryManagementView_Loaded(object sender, RoutedEventArgs e)
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
                MessageBox.Show($"Error loading inventory management view: {ex.Message}",
                       "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}