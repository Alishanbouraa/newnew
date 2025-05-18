// OfflinePOS.Cashier/Views/DrawerView.xaml.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Cashier.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Cashier.Views
{
    /// <summary>
    /// Interaction logic for DrawerView.xaml
    /// </summary>
    public partial class DrawerView : UserControl
    {
        private readonly DrawerViewModel _viewModel;
        // No direct logger reference needed here

        /// <summary>
        /// Initializes a new instance of the DrawerView class
        /// </summary>
        public DrawerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the DrawerView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">DrawerViewModel</param>
        public DrawerView(DrawerViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            // We'll use the ViewModel's logger instead of trying to get our own
        }

        /// <summary>
        /// Handles the UserControl.Loaded event to initialize the view and its ViewModel
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void DrawerView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Force refresh the drawer state
                if (_viewModel != null)
                {
                    // Initialize the view model
                    await _viewModel.InitializeAsync();

                    // Explicitly trigger property change to force UI refresh
                    _viewModel.RefreshDrawerState();
                }
            }
            catch (Exception ex)
            {
                // Log to ViewModel's logger if possible, otherwise use MessageBox directly
                MessageBox.Show($"Error initializing drawer view: {ex.Message}",
                               "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}