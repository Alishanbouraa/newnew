// OfflinePOS.Admin/ViewModels/ProductCatalogViewModel.cs
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
    /// Comprehensive ViewModel for managing the product catalog (products available for sale)
    /// </summary>
    public class ProductCatalogViewModel : ViewModelCommandBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly User _currentUser;

        #region Private Fields

        private ObservableCollection<Product> _catalogProducts;
        private ObservableCollection<Product> _selectedProducts;
        private ObservableCollection<Category> _categories;
        private Product _selectedProduct;
        private Category _selectedCategory;
        private string _searchText;
        private bool _isBusy;
        private string _statusMessage;
        private bool _isMultiSelectMode;
        private string _catalogViewMode = "Grid"; // Grid, List, Tiles

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of products available for sale in catalog
        /// </summary>
        public ObservableCollection<Product> CatalogProducts
        {
            get => _catalogProducts;
            set => SetProperty(ref _catalogProducts, value);
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
        /// Currently selected product in catalog
        /// </summary>
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    OnPropertyChanged(nameof(CanRemoveFromCatalog));
                    OnPropertyChanged(nameof(CanViewDetails));
                    OnPropertyChanged(nameof(CanEditProduct));
                }
            }
        }

        /// <summary>
        /// Selected category for filtering catalog
        /// </summary>
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = LoadCatalogProductsAsync();
                }
            }
        }

        /// <summary>
        /// Search text for filtering catalog products
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchCatalogProductsAsync();
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
                    OnPropertyChanged(nameof(CanBulkRemoveFromCatalog));
                }
            }
        }

        /// <summary>
        /// Current view mode for catalog display
        /// </summary>
        public string CatalogViewMode
        {
            get => _catalogViewMode;
            set => SetProperty(ref _catalogViewMode, value);
        }

        /// <summary>
        /// Available view modes for catalog display
        /// </summary>
        public ObservableCollection<string> ViewModes { get; } = new ObservableCollection<string>
        {
            "Grid", "List", "Tiles"
        };

        #endregion

        #region Computed Properties

        /// <summary>
        /// Determines if a single product can be removed from catalog
        /// </summary>
        public bool CanRemoveFromCatalog => SelectedProduct != null && !IsBusy;

        /// <summary>
        /// Determines if multiple products can be removed from catalog
        /// </summary>
        public bool CanBulkRemoveFromCatalog => IsMultiSelectMode && SelectedProducts.Any() && !IsBusy;

        /// <summary>
        /// Determines if product details can be viewed
        /// </summary>
        public bool CanViewDetails => SelectedProduct != null;

        /// <summary>
        /// Determines if product can be edited
        /// </summary>
        public bool CanEditProduct => SelectedProduct != null && !IsBusy;

        /// <summary>
        /// Gets total catalog product count
        /// </summary>
        public int TotalCatalogProducts => CatalogProducts?.Count ?? 0;

        #endregion

        #region Commands

        /// <summary>
        /// Command to load catalog products
        /// </summary>
        public ICommand LoadCatalogCommand { get; }

        /// <summary>
        /// Command to refresh the catalog view
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to search catalog products
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Command to clear search filters
        /// </summary>
        public ICommand ClearFiltersCommand { get; }

        /// <summary>
        /// Command to remove single product from catalog (back to inventory)
        /// </summary>
        public ICommand RemoveFromCatalogCommand { get; }

        /// <summary>
        /// Command to remove multiple selected products from catalog
        /// </summary>
        public ICommand BulkRemoveFromCatalogCommand { get; }

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
        /// Command to edit product information
        /// </summary>
        public ICommand EditProductCommand { get; }

        /// <summary>
        /// Command to manage product pricing
        /// </summary>
        public ICommand ManagePricingCommand { get; }

        /// <summary>
        /// Command to view product stock levels
        /// </summary>
        public ICommand ViewStockLevelsCommand { get; }

        /// <summary>
        /// Command to change catalog view mode
        /// </summary>
        public ICommand ChangeViewModeCommand { get; }

        /// <summary>
        /// Command to generate catalog report
        /// </summary>
        public ICommand GenerateCatalogReportCommand { get; }

        /// <summary>
        /// Command to export catalog to PDF/Excel
        /// </summary>
        public ICommand ExportCatalogCommand { get; }

        /// <summary>
        /// Command to navigate to inventory management
        /// </summary>
        public ICommand NavigateToInventoryCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ProductCatalogViewModel class
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="currentUser">Current authenticated user</param>
        public ProductCatalogViewModel(
            IServiceProvider serviceProvider,
            ILogger<ProductCatalogViewModel> logger,
            User currentUser)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Initialize collections
            CatalogProducts = new ObservableCollection<Product>();
            SelectedProducts = new ObservableCollection<Product>();
            Categories = new ObservableCollection<Category>();

            // Initialize commands
            LoadCatalogCommand = CreateAsyncCommand(LoadCatalogProductsAsync);
            RefreshCommand = CreateAsyncCommand(RefreshCatalogAsync);
            SearchCommand = CreateAsyncCommand(SearchCatalogProductsAsync);
            ClearFiltersCommand = CreateCommand(ClearFilters);
            RemoveFromCatalogCommand = CreateAsyncCommand(RemoveSingleFromCatalogAsync, CanExecuteRemoveSingle);
            BulkRemoveFromCatalogCommand = CreateAsyncCommand(BulkRemoveFromCatalogAsync, CanExecuteBulkRemove);
            ToggleMultiSelectCommand = CreateCommand(ToggleMultiSelect);
            ToggleProductSelectionCommand = CreateCommand(ToggleProductSelection);
            ViewProductDetailsCommand = CreateCommand(ViewProductDetails, CanExecuteViewDetails);
            EditProductCommand = CreateCommand(EditProduct, CanExecuteEditProduct);
            ManagePricingCommand = CreateCommand(ManagePricing);
            ViewStockLevelsCommand = CreateCommand(ViewStockLevels);
            ChangeViewModeCommand = CreateCommand(ChangeViewMode);
            GenerateCatalogReportCommand = CreateCommand(GenerateCatalogReport);
            ExportCatalogCommand = CreateCommand(ExportCatalog);
            NavigateToInventoryCommand = CreateCommand(NavigateToInventory);

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
                StatusMessage = "Initializing product catalog...";

                await LoadCategoriesAsync();
                await LoadCatalogProductsAsync();

                StatusMessage = "Product catalog initialized";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error initializing catalog: {ex.Message}";
                _logger.LogError(ex, "Error initializing ProductCatalogViewModel");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Private Command Methods

        /// <summary>
        /// Loads all catalog products using scoped service
        /// </summary>
        private async Task LoadCatalogProductsAsync(object parameter = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading catalog products...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                    IEnumerable<Product> products;
                    if (SelectedCategory != null && SelectedCategory.Id > 0)
                    {
                        products = await productService.GetCatalogProductsByCategoryAsync(SelectedCategory.Id);
                    }
                    else
                    {
                        products = await productService.GetCatalogProductsAsync();
                    }

                    CatalogProducts.Clear();
                    foreach (var product in products.OrderBy(p => p.Name))
                    {
                        CatalogProducts.Add(product);
                    }

                    StatusMessage = $"Loaded {CatalogProducts.Count} catalog products";
                    OnPropertyChanged(nameof(TotalCatalogProducts));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading catalog products: {ex.Message}";
                _logger.LogError(ex, "Error loading catalog products");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Refreshes all catalog data
        /// </summary>
        private async Task RefreshCatalogAsync(object parameter = null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Refreshing catalog...";

                // Clear current selections
                SelectedProducts.Clear();
                SelectedProduct = null;

                // Reload all data
                await LoadCatalogProductsAsync();

                StatusMessage = "Catalog refreshed successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing catalog: {ex.Message}";
                _logger.LogError(ex, "Error refreshing catalog");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Searches catalog products based on search text
        /// </summary>
        private async Task SearchCatalogProductsAsync(object parameter = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadCatalogProductsAsync();
                    return;
                }

                IsBusy = true;
                StatusMessage = "Searching catalog products...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var products = await productService.SearchCatalogProductsAsync(SearchText);

                    // Apply category filter if selected
                    if (SelectedCategory != null && SelectedCategory.Id > 0)
                    {
                        products = products.Where(p => p.CategoryId == SelectedCategory.Id);
                    }

                    CatalogProducts.Clear();
                    foreach (var product in products.OrderBy(p => p.Name))
                    {
                        CatalogProducts.Add(product);
                    }

                    StatusMessage = $"Found {CatalogProducts.Count} matching products";
                    OnPropertyChanged(nameof(TotalCatalogProducts));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching catalog products: {ex.Message}";
                _logger.LogError(ex, "Error searching catalog products");
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
            _ = LoadCatalogProductsAsync();
        }

        /// <summary>
        /// Removes a single product from catalog (back to inventory)
        /// </summary>
        private async Task RemoveSingleFromCatalogAsync(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"Remove '{product.Name}' from catalog and return to inventory?",
                    "Confirm Remove from Catalog",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                StatusMessage = $"Removing {product.Name} from catalog...";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    await productService.TransferToInventoryAsync(product.Id, _currentUser.Id);
                }

                // Remove from catalog list
                CatalogProducts.Remove(product);
                if (SelectedProduct == product)
                {
                    SelectedProduct = null;
                }

                StatusMessage = $"'{product.Name}' removed from catalog and returned to inventory";
                OnPropertyChanged(nameof(TotalCatalogProducts));

                _logger.LogInformation("Product {ProductId} removed from catalog by user {UserId}",
                    product.Id, _currentUser.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error removing product from catalog: {ex.Message}";
                _logger.LogError(ex, "Error removing single product from catalog");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Removes multiple selected products from catalog
        /// </summary>
        private async Task BulkRemoveFromCatalogAsync(object parameter)
        {
            if (!SelectedProducts.Any()) return;

            try
            {
                var result = MessageBox.Show(
                    $"Remove {SelectedProducts.Count} selected products from catalog and return to inventory?",
                    "Confirm Bulk Remove from Catalog",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                StatusMessage = $"Removing {SelectedProducts.Count} products from catalog...";

                var productIds = SelectedProducts.Select(p => p.Id).ToList();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var transferredCount = await productService.BulkTransferToInventoryAsync(productIds, _currentUser.Id);

                    // Remove transferred products from catalog list
                    var removedProducts = SelectedProducts.ToList();
                    foreach (var product in removedProducts)
                    {
                        CatalogProducts.Remove(product);
                    }

                    SelectedProducts.Clear();
                    IsMultiSelectMode = false;

                    StatusMessage = $"{transferredCount} products removed from catalog and returned to inventory";
                    OnPropertyChanged(nameof(TotalCatalogProducts));

                    _logger.LogInformation("{Count} products bulk removed from catalog by user {UserId}",
                        transferredCount, _currentUser.Id);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error bulk removing products from catalog: {ex.Message}";
                _logger.LogError(ex, "Error bulk removing products from catalog");
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
                OnPropertyChanged(nameof(CanBulkRemoveFromCatalog));
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
                // Implementation would show comprehensive product details dialog
                var details = $"Product Details:\n\n" +
                             $"Name: {product.Name}\n" +
                             $"Category: {product.Category?.Name}\n" +
                             $"Item Barcode: {product.ItemBarcode}\n" +
                             $"Box Barcode: {product.BoxBarcode}\n" +
                             $"Items per Box: {product.ItemsPerBox}\n" +
                             $"Item Sale Price: ${product.ItemSalePrice:F2}\n" +
                             $"Box Sale Price: ${product.BoxSalePrice:F2}\n" +
                             $"Available Since: {product.AvailableForSaleDate:yyyy-MM-dd}";

                MessageBox.Show(details, "Product Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing product details: {ex.Message}";
                _logger.LogError(ex, "Error viewing product details");
            }
        }

        /// <summary>
        /// Opens product edit dialog
        /// </summary>
        private void EditProduct(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                // Implementation would open product edit dialog
                StatusMessage = $"Opening edit dialog for {product.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening product edit: {ex.Message}";
                _logger.LogError(ex, "Error opening product edit dialog");
            }
        }

        /// <summary>
        /// Opens pricing management for selected product
        /// </summary>
        private void ManagePricing(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                // Implementation would open pricing management dialog
                StatusMessage = $"Opening pricing management for {product.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening pricing management: {ex.Message}";
                _logger.LogError(ex, "Error opening pricing management");
            }
        }

        /// <summary>
        /// Views stock levels for selected product
        /// </summary>
        private void ViewStockLevels(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null) return;

            try
            {
                // Implementation would show stock level information
                StatusMessage = $"Viewing stock levels for {product.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing stock levels: {ex.Message}";
                _logger.LogError(ex, "Error viewing stock levels");
            }
        }

        /// <summary>
        /// Changes the catalog view mode
        /// </summary>
        private void ChangeViewMode(object parameter)
        {
            if (parameter is string viewMode && ViewModes.Contains(viewMode))
            {
                CatalogViewMode = viewMode;
                StatusMessage = $"Changed to {viewMode} view";
            }
        }

        /// <summary>
        /// Generates comprehensive catalog report
        /// </summary>
        private void GenerateCatalogReport(object parameter)
        {
            try
            {
                StatusMessage = "Generating catalog report...";

                // Implementation would generate detailed catalog report
                MessageBox.Show("Catalog report generation not implemented yet.",
                    "Report Generation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating catalog report: {ex.Message}";
                _logger.LogError(ex, "Error generating catalog report");
            }
        }

        /// <summary>
        /// Exports catalog to external format
        /// </summary>
        private void ExportCatalog(object parameter)
        {
            try
            {
                StatusMessage = "Exporting catalog...";

                // Implementation would export catalog to PDF/Excel
                MessageBox.Show("Catalog export not implemented yet.",
                    "Export Catalog", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting catalog: {ex.Message}";
                _logger.LogError(ex, "Error exporting catalog");
            }
        }

        /// <summary>
        /// Navigates to inventory management view
        /// </summary>
        private void NavigateToInventory(object parameter)
        {
            try
            {
                // Implementation would navigate to inventory management
                StatusMessage = "Navigating to inventory management...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to inventory: {ex.Message}";
                _logger.LogError(ex, "Error navigating to inventory management");
            }
        }

        #endregion

        #region Command Validation Methods

        private bool CanExecuteRemoveSingle(object parameter)
        {
            return !IsBusy && (parameter is Product || SelectedProduct != null);
        }

        private bool CanExecuteBulkRemove(object parameter)
        {
            return !IsBusy && IsMultiSelectMode && SelectedProducts.Any();
        }

        private bool CanExecuteViewDetails(object parameter)
        {
            return parameter is Product || SelectedProduct != null;
        }

        private bool CanExecuteEditProduct(object parameter)
        {
            return !IsBusy && (parameter is Product || SelectedProduct != null);
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

        #endregion
    }
}