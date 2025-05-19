using OfflinePOS.Admin.ViewModels;
using System;
using System.Windows;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for ProductDialogView.xaml
    /// </summary>
    public partial class ProductDialogView : Window
    {
        private readonly ProductDialogViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the ProductDialogView class
        /// </summary>
        public ProductDialogView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the ProductDialogView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel for the dialog</param>
        public ProductDialogView(ProductDialogViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to close request event from ViewModel
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += ViewModel_CloseRequested;

                // Load data after initialization
                this.Loaded += async (s, e) =>
                {
                    try
                    {
                        await _viewModel.LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading data: {ex.Message}",
                                        "Data Loading Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                };
            }
        }

        /// <summary>
        /// Handles the ViewModel's close request event
        /// </summary>
        private void ViewModel_CloseRequested(object sender, bool dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        /// <summary>
        /// Cleans up resources when the window is closed
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= ViewModel_CloseRequested;
            }

            base.OnClosed(e);
        }
    }
}