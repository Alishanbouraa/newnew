using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for product management
    /// </summary>
    public class ProductViewModel : InventoryViewModelBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ISupplierService _supplierService;
        private readonly IServiceProvider _serviceProvider;
        private Product _selectedProduct;
        private Category _selectedCategory;
        private ObservableCollection<Category> _categories;

        /// <summary>
        /// Currently selected product
        /// </summary>
        public override Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        /// <summary>
        /// Currently selected category for filtering
        /// </summary>
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    // Refresh products when category changes
                    SearchProducts(null);
                }
            }
        }

        /// <summary>
        /// Collection of available categories
        /// </summary>
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        /// <summary>
        /// Command for adding a new product
        /// </summary>
        public ICommand AddProductCommand { get; }

        /// <summary>
        /// Command for editing a product
        /// </summary>
        public ICommand EditProductCommand { get; }

        /// <summary>
        /// Command for deleting a product
        /// </summary>
        public ICommand DeleteProductCommand { get; }

        /// <summary>
        /// Command for managing stock
        /// </summary>
        public ICommand ManageStockCommand { get; }

        /// <summary>
        /// Command for managing barcodes
        /// </summary>
        public ICommand ManageBarcodesCommand { get; }

        /// <summary>
        /// Command for import/export
        /// </summary>
        public ICommand ImportExportCommand { get; }

        /// <summary>
        /// Initializes a new instance of the ProductViewModel class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="stockService">Stock service</param>
        /// <param name="categoryService">Category service</param>
        /// <param name="supplierService">Supplier service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        public ProductViewModel(
            IProductService productService,
            IStockService stockService,
            ICategoryService categoryService,
            ISupplierService supplierService,
            ILogger<ProductViewModel> logger,
            User currentUser,
            IServiceProvider serviceProvider)
            : base(productService, stockService, logger, currentUser)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize collections
            Categories = new ObservableCollection<Category>();

            // Initialize commands
            AddProductCommand = CreateCommand(AddProduct);
            EditProductCommand = CreateCommand(EditProduct, CanEditProduct);
            DeleteProductCommand = CreateCommand(DeleteProduct, CanDeleteProduct);
            ManageStockCommand = CreateCommand(ManageStock);
            ManageBarcodesCommand = CreateCommand(ManageBarcodes);
            ImportExportCommand = CreateCommand(ImportExport);
        }

        /// <summary>
        /// Loads data from repositories
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public override async Task LoadDataAsync()
        {
            await LoadCategories();
            await base.LoadDataAsync();
        }

        /// <summary>
        /// Loads the list of categories
        /// </summary>
        private async Task LoadCategories()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading categories...";

                var categories = await _categoryService.GetCategoriesByTypeAsync("Product");

                Categories.Clear();

                // Add "All Categories" option
                Categories.Add(new Category { Id = 0, Name = "All Categories" });

                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Select "All Categories" by default
                SelectedCategory = Categories.First();

                StatusMessage = "Categories loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading categories: {ex.Message}";
                _logger.LogError(ex, "Error loading categories");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Overrides the search products method to include category filtering
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        protected override async void SearchProducts(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Searching products...";

                IEnumerable<Product> products;

                if (SelectedCategory != null && SelectedCategory.Id > 0)
                {
                    // Filter by category and search text
                    if (string.IsNullOrWhiteSpace(SearchText))
                    {
                        products = await _productService.GetProductsByCategoryAsync(SelectedCategory.Id);
                    }
                    else
                    {
                        // Get all products matching the search text
                        var searchResults = await _productService.SearchProductsAsync(SearchText);
                        // Filter by category
                        products = searchResults.Where(p => p.CategoryId == SelectedCategory.Id);
                    }
                }
                else
                {
                    // No category filter, just search by text
                    if (string.IsNullOrWhiteSpace(SearchText))
                    {
                        products = await _productService.GetAllProductsAsync();
                    }
                    else
                    {
                        products = await _productService.SearchProductsAsync(SearchText);
                    }
                }

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
                _logger.LogError(ex, "Error searching products with text: {SearchText}", SearchText);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Opens the Add Product dialog
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void AddProduct(object parameter)
        {
            try
            {
                // Create the dialog view model
                var dialogViewModel = new ProductDialogViewModel(
                    _productService,
                    _categoryService,
                    _supplierService,
                    (ILogger<ProductDialogViewModel>)_serviceProvider.GetService(typeof(ILogger<ProductDialogViewModel>)), // Get typed logger from service provider
                    _currentUser);

                // Create and show the dialog
                var dialog = new ProductDialogView(dialogViewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh products if dialog was successful
                if (result == true)
                {
                    SearchProducts(null);
                    StatusMessage = "Product added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding product: {ex.Message}";
                _logger.LogError(ex, "Error adding product");
            }
        }

        /// <summary>
        /// Opens the Edit Product dialog for the selected product
        /// </summary>
        /// <param name="parameter">Product to edit</param>
        private async void EditProduct(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null)
                return;

            try
            {
                // Get the complete product with navigation properties
                var completeProduct = await _productService.GetProductByIdAsync(product.Id);
                if (completeProduct == null)
                {
                    StatusMessage = "Product not found";
                    return;
                }

                // Create the dialog view model
                var dialogViewModel = new ProductDialogViewModel(
         _productService,
         _categoryService,
         _supplierService,
         (ILogger<ProductDialogViewModel>)_serviceProvider.GetService(typeof(ILogger<ProductDialogViewModel>)), // Get typed logger from service provider
         _currentUser,
         completeProduct);

                // Create and show the dialog
                var dialog = new ProductDialogView(dialogViewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh products if dialog was successful
                if (result == true)
                {
                    SearchProducts(null);
                    StatusMessage = "Product updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing product: {ex.Message}";
                _logger.LogError(ex, "Error editing product {ProductId}", product.Id);
            }
        }

        /// <summary>
        /// Deletes the selected product after confirmation
        /// </summary>
        /// <param name="parameter">Product to delete</param>
        private async void DeleteProduct(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;
            if (product == null)
                return;

            var result = MessageBox.Show($"Are you sure you want to delete product '{product.Name}'?",
                                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = "Deleting product...";

                    bool success = await _productService.DeleteProductAsync(product.Id);

                    if (success)
                    {
                        Products.Remove(product);
                        StatusMessage = $"Product '{product.Name}' deleted successfully";
                    }
                    else
                    {
                        StatusMessage = $"Failed to delete product '{product.Name}'";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error deleting product: {ex.Message}";
                    _logger.LogError(ex, "Error deleting product {ProductId}", product.Id);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Opens the Stock Management view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ManageStock(object parameter)
        {
            try
            {
                // Get the StockManagementView from the service provider
                var stockView = _serviceProvider.GetService(typeof(StockManagementView)) as UserControl;
                if (stockView != null)
                {
                    // Get the parent window's content control or frame
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Find the content control by name
                        var contentControl = mainWindow.FindName("MainContent") as ContentControl;
                        if (contentControl != null)
                        {
                            contentControl.Content = stockView;
                            return;
                        }
                    }
                }

                // Fallback if any of the above fails
                MessageBox.Show("Navigation to Stock Management view not implemented yet.",
                               "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Stock Management: {ex.Message}";
                _logger.LogError(ex, "Error navigating to Stock Management");
            }
        }

        /// <summary>
        /// Opens the Barcode Management view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ManageBarcodes(object parameter)
        {
            try
            {
                // Get the BarcodeManagementView from the service provider
                var barcodeView = _serviceProvider.GetService(typeof(BarcodeManagementView)) as UserControl;
                if (barcodeView != null)
                {
                    // Get the parent window's content control or frame
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Find the content control by name
                        var contentControl = mainWindow.FindName("MainContent") as ContentControl;
                        if (contentControl != null)
                        {
                            contentControl.Content = barcodeView;
                            return;
                        }
                    }
                }

                // Fallback if any of the above fails
                MessageBox.Show("Navigation to Barcode Management view not implemented yet.",
                               "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Barcode Management: {ex.Message}";
                _logger.LogError(ex, "Error navigating to Barcode Management");
            }
        }

        /// <summary>
        /// Opens the Import/Export view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ImportExport(object parameter)
        {
            try
            {
                // Get the ProductImportExportView from the service provider
                var importExportView = _serviceProvider.GetService(typeof(ProductImportExportView)) as UserControl;
                if (importExportView != null)
                {
                    // Get the parent window's content control or frame
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Find the content control by name
                        var contentControl = mainWindow.FindName("MainContent") as ContentControl;
                        if (contentControl != null)
                        {
                            contentControl.Content = importExportView;
                            return;
                        }
                    }
                }

                // Fallback if any of the above fails
                MessageBox.Show("Navigation to Import/Export view not implemented yet.",
                               "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Import/Export: {ex.Message}";
                _logger.LogError(ex, "Error navigating to Import/Export");
            }
        }

        /// <summary>
        /// Determines if a product can be edited
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if a product can be edited, false otherwise</returns>
        private bool CanEditProduct(object parameter)
        {
            return parameter is Product || SelectedProduct != null;
        }

        /// <summary>
        /// Determines if a product can be deleted
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if a product can be deleted, false otherwise</returns>
        private bool CanDeleteProduct(object parameter)
        {
            return parameter is Product || SelectedProduct != null;
        }
    }
}