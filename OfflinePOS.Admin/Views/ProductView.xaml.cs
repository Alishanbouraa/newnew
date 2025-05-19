using OfflinePOS.Admin.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OfflinePOS.Admin.Views
{
    /// <summary>
    /// Interaction logic for ProductView.xaml
    /// </summary>
    public partial class ProductView : UserControl
    {
        private readonly ProductViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the ProductView class
        /// </summary>
        public ProductView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the ProductView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel for the view</param>
        public ProductView(ProductViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the UserControl.Loaded event
        /// </summary>
        private async void ProductView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading product view: {ex.Message}",
                       "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}