// File: OfflinePOS.Cashier/Views/DrawerView.xaml.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Cashier.ViewModels;
using System;
using System.Threading.Tasks;
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
                if (_viewModel == null)
                {
                    MessageBox.Show("ViewModel not initialized.", "Initialization Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Initialize the view model
                await _viewModel.InitializeAsync();

                // Give the UI a moment to settle before forcing refresh
                await Task.Delay(100);

                // Force refresh UI state
                _viewModel.RefreshDrawerState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing drawer view: {ex.Message}",
                       "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}