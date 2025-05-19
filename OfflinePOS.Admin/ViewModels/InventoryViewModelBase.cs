using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;


namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// Base class for inventory-related ViewModels
    /// </summary>
    public abstract class InventoryViewModelBase : ViewModelCommandBase
    {
        protected readonly IProductService _productService;
        protected readonly IStockService _stockService;
        protected readonly User _currentUser;

        private ObservableCollection<Product> _products;
        private Product _selectedProduct;
        private string _searchText;
        private bool _isBusy;
        private string _statusMessage;

        /// <summary>
        /// Collection of products
        /// </summary>
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        /// <summary>
        /// Currently selected product
        /// </summary>
        public virtual Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        /// <summary>
        /// Search text for finding products
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Flag indicating if a background operation is in progress
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Status message to display
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Command for searching products
        /// </summary>
        public ICommand SearchProductsCommand { get; }

        /// <summary>
        /// Command for refreshing the product list
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Initializes a new instance of the InventoryViewModelBase class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="stockService">Stock service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        protected InventoryViewModelBase(
            IProductService productService,
            IStockService stockService,
            ILogger logger,
            User currentUser)
            : base(logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Initialize collections
            Products = new ObservableCollection<Product>();

            // Initialize commands
            SearchProductsCommand = CreateCommand(SearchProducts);
            RefreshCommand = CreateCommand(async _ => await LoadDataAsync());
        }

        /// <summary>
        /// Loads products from the repository
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public virtual async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading products...";

                var products = await _productService.GetAllProductsAsync();

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                StatusMessage = $"Loaded {Products.Count} products";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading products: {ex.Message}";
                _logger.LogError(ex, "Error loading products");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Searches for products matching the search text
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        protected virtual async void SearchProducts(object parameter)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadDataAsync();
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Searching products...";

                var products = await _productService.SearchProductsAsync(SearchText);

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                StatusMessage = $"Found {Products.Count} products";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching products: {ex.Message}";
                _logger.LogError(ex, "Error searching products with term: {SearchTerm}", SearchText);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}