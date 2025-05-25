// OfflinePOS.Admin/ViewModels/InventoryManagementViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// Comprehensive ViewModel for managing product inventory with transfer capabilities
    /// </summary>
    public class InventoryManagementViewModel : ViewModelCommandBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly User _currentUser;

        #region Private Fields

        private ObservableCollection<Product> _inventoryProducts;
        private ObservableCollection<Product> _selectedProducts;
        private ObservableCollection<Category> _categories;
        private Product _selectedProduct;
        private Category _selectedCategory;
        private string _searchText;
        private bool _isBusy;
        private string _statusMessage;
        private InventoryStatistics _statistics;
        private bool _isMultiSelectMode;

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of products currently in inventory
        /// </summary>
        public ObservableCollection<Product> InventoryProducts
        {
            get => _inventoryProducts;
            set => SetProperty(ref _inventoryProducts, value);
        }

        /// <summary>
        /// Collection of selected products for bulk operations
        /// </summary>
        public ObservableCollection<Product> SelectedProducts
        {
            get => _selectedProducts;
            set => SetProperty(ref _selectedProducts, value);
        }

        /// <summary>
        /// Available categories for filtering
        /// </summary>
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        /// <summary>
        /// Currently selected product
        /// </summary>
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    OnPropertyChanged(nameof(CanTransferSingle));
                    OnPropertyChanged(nameof(CanViewDetails));
                }
            }
        }

        /// <summary>
        /// Selected category for filtering
        /// </summary>
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = LoadInventoryProductsAsync();
                }
            }
        }

        /// <summary>
        /// Search text for filtering products
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchInventoryProductsAsync();
                }
            }
        }

        /// <summary>
        /// Flag indicating if background operation is in progress
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Status message for user feedback
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Inventory statistics summary
        /// </summary>
        public InventoryStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        /// <summary>
        /// Flag indicating if multi-select mode is enabled
        /// </summary>
        public bool IsMultiSelectMode
        {
            get => _isMultiSelectMode;
            set
            {
                if (SetProperty(ref _isMultiSelectMode, value))
                {
                    if (!value)
                    {
                        SelectedProducts.Clear();
                    }
                    OnPropertyChanged(nameof(CanTransferMultiple));
                }
            }
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Determines if a single product can be transferred to catalog
        /// </summary>
        public bool CanTransferSingle => SelectedProduct != null && !IsBusy;

        /// <summary>
        /// Determines if multiple products can be transferred to catalog
        /// </summary>
        public bool CanTransferMultiple => IsMultiSelectMode && SelectedProducts.Any() && !IsBusy;

        /// <summary>
        /// Determines if product details can be viewed
        /// </summary>
        public bool CanViewDetails => SelectedProduct != null;

        /// <summary>
        /// Gets the count of ready products for transfer
        /// </summary>
        public int ReadyForCatalogCount => Statistics?.ProductsReadyForCatalog ?? 0;

        #endregion

        #region Commands

        /// <summary>
        /// Command to load inventory products
        /// </summary>
        public ICommand LoadInventoryCommand { get; }

        /// <summary>
        /// Command to refresh the inventory view
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to search inventory products
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Command to clear search filters
        /// </summary>
        public ICommand ClearFiltersCommand { get; }

        /// <summary>
        /// Command to transfer single product to catalog
        /// </summary>
        public ICommand TransferToCatalogCommand { get; }

        /// <summary>
        /// Command to transfer multiple selected products to catalog
        /// </summary>
        public ICommand BulkTransferToCatalogCommand { get; }

        /// <summary>
        /// Command to transfer all ready products to catalog
        /// </summary>
        public ICommand TransferAllReadyCommand { get; }

        /// <summary>
        /// Command to toggle multi-select mode
        /// </summary>
        public ICommand ToggleMultiSelectCommand { get; }

        /// <summary>
        /// Command to select/deselect a product for bulk operations
        /// </summary>
        public ICommand ToggleProductSelectionCommand { get; }

        /// <summary>
        /// Command to view product details
        /// </summary>
        public ICommand ViewProductDetailsCommand { get; }

        /// <summary>
        /// Command to manage product stock
        /// </summary>
        public ICommand ManageStockCommand { get; }

        /// <summary>
        /// Command to show products ready for catalog transfer
        /// </summary>
        public ICommand ShowReadyProductsCommand { get; }

        /// <summary>
        /// Command to generate inventory report
        /// </summary>
        public ICommand GenerateReportCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the InventoryManagementViewModel class
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="currentUser">Current authenticated user</param>
        public InventoryManagementViewModel(
            IServiceProvider serviceProvider,
            ILogger<InventoryManagementViewModel> logger,
            User currentUser)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Initialize collections
            InventoryProducts = new ObservableCollection<Product>();
            SelectedProducts = new ObservableCollection<Product>();
            Categories = new ObservableCollection<Category>();

            // Initialize commands
            LoadInventoryCommand = CreateAsyncCommand(LoadInventoryProductsAsync);
            RefreshCommand = CreateAsyncCommand(RefreshInventoryAsync);
            SearchCommand = CreateAsyncCommand(SearchInventoryProductsAsync);
            ClearFiltersCommand = CreateCommand(ClearFilters);
            TransferToCatalogCommand = CreateAsyncCommand(TransferSingleToCatalogAsync, CanExecuteTransferSingle);
            BulkTransferToCatalogCommand = CreateAsyncCommand(BulkTransferToCatalogAsync, CanExecuteBulkTransfer);
            TransferAllReadyCommand = CreateAsyncCommand(TransferAllReadyToCatalogAsync);
            ToggleMultiSelectCommand = CreateCommand(ToggleMultiSelect);
            ToggleProductSelectionCommand = CreateCommand(ToggleProductSelection);
            ViewProductDetailsCommand = CreateCommand(ViewProductDetails, CanExecuteViewDetails);
            ManageStockCommand = CreateCommand(ManageStock);
            ShowReadyProductsCommand = CreateAsyncCommand(ShowReadyProductsAsync);
            GenerateReportCommand = CreateCommand(GenerateReport);

            // Load initial data
            _ = InitializeAsync();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the ViewModel with data loading
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Initializing inventory management...";

                await LoadCategoriesAsync();
                await LoadInventoryProductsAsync();
                await LoadStatisticsAsync();

                StatusMessage = "Inventory management initialized";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error initializing: {ex.Message}";
                _logger.LogError(ex, "Error initializing InventoryManagementViewModel");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Private Command Methods

        /// <summary>
        /// Loads all inventory products using scoped service
        /// </summary>
        private async Task LoadInventoryProductsAsync(object parameter = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading inventory products...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                    IEnumerable<Product> products;
                    if (SelectedCategory != null && SelectedCategory.Id > 0)
                    {
                        products = await productService.GetInventoryProductsByCategoryAsync(SelectedCategory.Id);
                    }
                    else
                    {
                        products = await productService.GetInventoryProductsAsync();
                    }

                    InventoryProducts.Clear();
                    foreach (var product in products.OrderBy(p => p.Name))
                    {
                        InventoryProducts.Add(product);
                    }

                    StatusMessage = $"Loaded {InventoryProducts.Count} inventory products";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading inventory products: {ex.Message}";
                _logger.LogError(ex, "Error loading inventory products");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Refreshes all inventory data
        /// </summary>
        private async Task RefreshInventoryAsync(object parameter = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Refreshing inventory...";

                // Clear current selections
                SelectedProducts.Clear();
                SelectedProduct = null;

                // Reload all data
                await LoadInventoryProductsAsync();
                await LoadStatisticsAsync();

                StatusMessage = "Inventory refreshed successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing inventory: {ex.Message}";
                _logger.LogError(ex, "Error refreshing inventory");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Searches inventory products based on search text
        /// </summary>
        private async Task SearchInventoryProductsAsync(object parameter = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadInventoryProductsAsync();
                    return;
                }

                IsBusy = true;
                StatusMessage = "Searching inventory products...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var products = await productService.SearchInventoryProductsAsync(SearchText);

                    // Apply category filter if selected
                    if (SelectedCategory != null && SelectedCategory.Id > 0)
                    {
                        products = products.Where(p => p.CategoryId == SelectedCategory.Id);
                    }

                    InventoryProducts.Clear();
                    foreach (var product in products.OrderBy(p => p.Name))
                    {
                        InventoryProducts.Add(product);
                    }

                    StatusMessage = $"Found {InventoryProducts.Count} matching products";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching products: {ex.Message}";
                _logger.LogError(ex, "Error searching inventory products");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Clears all search filters and reloads products
        /// </summary>
        private void ClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedCategory = Categories.FirstOrDefault();
            _ = LoadInventoryProductsAsync();
        }

        /// <summary>
        /// Transfers a single product to catalog
        /// </summary>
        private async Task TransferSingleToCatalogAsync(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"Transfer '{product.Name}' to catalog for sale?",
                    "Confirm Transfer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                StatusMessage = $"Transferring {product.Name} to catalog...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    await productService.TransferToCatalogAsync(product.Id, _currentUser.Id);
                }

                // Remove from inventory list
                InventoryProducts.Remove(product);
                if (SelectedProduct == product)
                {
                    SelectedProduct = null;
                }

                await LoadStatisticsAsync();
                StatusMessage = $"'{product.Name}' transferred to catalog successfully";

                _logger.LogInformation("Product {ProductId} transferred to catalog by user {UserId}",
                    product.Id, _currentUser.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error transferring product: {ex.Message}";
                _logger.LogError(ex, "Error transferring single product to catalog");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Transfers multiple selected products to catalog
        /// </summary>
        private async Task BulkTransferToCatalogAsync(object parameter)
        {
            if (!SelectedProducts.Any()) return;

            try
            {
                var result = MessageBox.Show(
                    $"Transfer {SelectedProducts.Count} selected products to catalog?",
                    "Confirm Bulk Transfer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                StatusMessage = $"Transferring {SelectedProducts.Count} products to catalog...";

                var productIds = SelectedProducts.Select(p => p.Id).ToList();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var transferredCount = await productService.BulkTransferToCatalogAsync(productIds, _currentUser.Id);

                    // Remove transferred products from inventory list
                    var transferredProducts = SelectedProducts.ToList();
                    foreach (var product in transferredProducts)
                    {
                        InventoryProducts.Remove(product);
                    }

                    SelectedProducts.Clear();
                    IsMultiSelectMode = false;
                    await LoadStatisticsAsync();

                    StatusMessage = $"{transferredCount} products transferred to catalog successfully";

                    _logger.LogInformation("{Count} products bulk transferred to catalog by user {UserId}",
                        transferredCount, _currentUser.Id);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error bulk transferring products: {ex.Message}";
                _logger.LogError(ex, "Error bulk transferring products to catalog");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Transfers all products ready for catalog
        /// </summary>
        private async Task TransferAllReadyToCatalogAsync(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Finding products ready for catalog...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var readyProducts = await productService.GetProductsReadyForCatalogAsync();
                    var readyProductsList = readyProducts.ToList();

                    if (!readyProductsList.Any())
                    {
                        StatusMessage = "No products ready for catalog transfer";
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Transfer {readyProductsList.Count} ready products to catalog?",
                        "Confirm Transfer All Ready",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    StatusMessage = $"Transferring {readyProductsList.Count} ready products...";

                    var productIds = readyProductsList.Select(p => p.Id);
                    var transferredCount = await productService.BulkTransferToCatalogAsync(productIds, _currentUser.Id);

                    // Refresh inventory view
                    await LoadInventoryProductsAsync();
                    await LoadStatisticsAsync();

                    StatusMessage = $"{transferredCount} ready products transferred to catalog";

                    _logger.LogInformation("{Count} ready products transferred to catalog by user {UserId}",
                        transferredCount, _currentUser.Id);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error transferring ready products: {ex.Message}";
                _logger.LogError(ex, "Error transferring all ready products");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Toggles multi-select mode for bulk operations
        /// </summary>
        private void ToggleMultiSelect(object parameter)
        {
            IsMultiSelectMode = !IsMultiSelectMode;
            if (!IsMultiSelectMode)
            {
                SelectedProducts.Clear();
            }
        }

        /// <summary>
        /// Toggles selection of a product for bulk operations
        /// </summary>
        private void ToggleProductSelection(object parameter)
        {
            if (parameter is Product product)
            {
                if (SelectedProducts.Contains(product))
                {
                    SelectedProducts.Remove(product);
                }
                else
                {
                    SelectedProducts.Add(product);
                }
                OnPropertyChanged(nameof(CanTransferMultiple));
            }
        }

        /// <summary>
        /// Views detailed information for selected product
        /// </summary>
        private void ViewProductDetails(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                // Implementation would show product details dialog
                MessageBox.Show($"Product Details:\n\nName: {product.Name}\nCategory: {product.Category?.Name}\nBarcode: {product.ItemBarcode}\nTracking: {product.TrackInventory}",
                    "Product Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing product details: {ex.Message}";
                _logger.LogError(ex, "Error viewing product details");
            }
        }

        /// <summary>
        /// Opens stock management for selected product
        /// </summary>
        private void ManageStock(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                // Implementation would navigate to stock management view
                StatusMessage = $"Opening stock management for {product.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening stock management: {ex.Message}";
                _logger.LogError(ex, "Error opening stock management");
            }
        }

        /// <summary>
        /// Shows products ready for catalog transfer
        /// </summary>
        private async Task ShowReadyProductsAsync(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading products ready for catalog...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var readyProducts = await productService.GetProductsReadyForCatalogAsync();

                    InventoryProducts.Clear();
                    foreach (var product in readyProducts.OrderBy(p => p.Name))
                    {
                        InventoryProducts.Add(product);
                    }

                    StatusMessage = $"Showing {InventoryProducts.Count} products ready for catalog";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading ready products: {ex.Message}";
                _logger.LogError(ex, "Error loading products ready for catalog");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Generates inventory report
        /// </summary>
        private void GenerateReport(object parameter)
        {
            try
            {
                // Implementation would generate comprehensive inventory report
                StatusMessage = "Generating inventory report...";

                // Placeholder - would implement actual report generation
                MessageBox.Show("Inventory report generation not implemented yet.",
                    "Report Generation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating report: {ex.Message}";
                _logger.LogError(ex, "Error generating inventory report");
            }
        }

        #endregion

        #region Command Validation Methods

        private bool CanExecuteTransferSingle(object parameter)
        {
            return !IsBusy && (parameter is Product || SelectedProduct != null);
        }

        private bool CanExecuteBulkTransfer(object parameter)
        {
            return !IsBusy && IsMultiSelectMode && SelectedProducts.Any();
        }

        private bool CanExecuteViewDetails(object parameter)
        {
            return parameter is Product || SelectedProduct != null;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Loads available categories using scoped service
        /// </summary>
        private async Task LoadCategoriesAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                    var categories = await categoryService.GetCategoriesByTypeAsync("Product");

                    Categories.Clear();
                    Categories.Add(new Category { Id = 0, Name = "All Categories" });

                    foreach (var category in categories.OrderBy(c => c.Name))
                    {
                        Categories.Add(category);
                    }

                    SelectedCategory = Categories.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                throw;
            }
        }

        /// <summary>
        /// Loads inventory statistics using scoped service
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    Statistics = await productService.GetInventoryStatisticsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory statistics");
                // Don't throw - statistics are not critical for main functionality
            }
        }

        #endregion
    }
}