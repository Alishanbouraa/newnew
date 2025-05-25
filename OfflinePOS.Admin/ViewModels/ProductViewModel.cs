// File: OfflinePOS.Admin/ViewModels/ProductViewModel.cs
using Microsoft.Extensions.DependencyInjection;
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
    /// ViewModel for product management with proper service scoping to prevent DbContext concurrency issues
    /// </summary>
    public class ProductViewModel : InventoryViewModelBase
    {
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

        #region Commands

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

        #endregion

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
        /// Loads data from repositories using proper service scoping
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public override async Task LoadDataAsync()
        {
            await LoadCategories();
            await base.LoadDataAsync();
        }

        /// <summary>
        /// Loads categories using a dedicated service scope
        /// </summary>
        private async Task LoadCategories()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading categories...";

                // Create a dedicated service scope for category loading
                using (var scope = _serviceProvider.CreateScope())
                {
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                    var categories = await categoryService.GetCategoriesByTypeAsync("Product");

                    Categories.Clear();
                    Categories.Add(new Category { Id = 0, Name = "All Categories" });

                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }

                    SelectedCategory = Categories.First();
                    StatusMessage = "Categories loaded";
                }
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
        /// Searches products with category filtering using proper service scoping
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        protected override async void SearchProducts(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Searching products...";

                // Create a dedicated service scope for product search
                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    IEnumerable<Product> products;

                    if (SelectedCategory != null && SelectedCategory.Id > 0)
                    {
                        if (string.IsNullOrWhiteSpace(SearchText))
                        {
                            products = await productService.GetProductsByCategoryAsync(SelectedCategory.Id);
                        }
                        else
                        {
                            var searchResults = await productService.SearchProductsAsync(SearchText);
                            products = searchResults.Where(p => p.CategoryId == SelectedCategory.Id);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(SearchText))
                        {
                            products = await productService.GetAllProductsAsync();
                        }
                        else
                        {
                            products = await productService.SearchProductsAsync(SearchText);
                        }
                    }

                    Products.Clear();
                    foreach (var product in products)
                    {
                        Products.Add(product);
                    }

                    StatusMessage = $"Found {Products.Count} products";
                }
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
        private async void AddProduct(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Preparing to add product...";

                // Create dialog view model using the factory
                var dialogViewModelFactory = _serviceProvider.GetRequiredService<Func<Product, ProductDialogViewModel>>();
                var dialogViewModel = dialogViewModelFactory(null);

                // Load data for the dialog
                await dialogViewModel.LoadDataAsync();

                // Create and show the dialog
                var dialog = new ProductDialogView(dialogViewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    await LoadDataAsync();
                    StatusMessage = "Product added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding product: {ex.Message}";
                _logger.LogError(ex, "Error adding a new product");
            }
            finally
            {
                IsBusy = false;
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
                IsBusy = true;
                StatusMessage = "Loading product details...";

                Product completeProduct = null;

                // Get the complete product using a dedicated service scope
                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    completeProduct = await productService.GetProductByIdAsync(product.Id);

                    if (completeProduct == null)
                    {
                        StatusMessage = "Product not found";
                        return;
                    }
                }

                // Create dialog view model using the factory
                var dialogViewModelFactory = _serviceProvider.GetRequiredService<Func<Product, ProductDialogViewModel>>();
                var dialogViewModel = dialogViewModelFactory(completeProduct);

                // Load data for the dialog
                await dialogViewModel.LoadDataAsync();

                // Create and show the dialog
                var dialog = new ProductDialogView(dialogViewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

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
            finally
            {
                IsBusy = false;
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

                    bool success = false;

                    // Delete product using a dedicated service scope
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                        success = await productService.DeleteProductAsync(product.Id);
                    }

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
        /// Navigates to the Stock Management view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ManageStock(object parameter)
        {
            try
            {
                NavigateToView<StockManagementView>("Stock Management");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Stock Management: {ex.Message}";
                _logger.LogError(ex, "Error navigating to Stock Management");
            }
        }

        /// <summary>
        /// Navigates to the Barcode Management view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ManageBarcodes(object parameter)
        {
            try
            {
                NavigateToView<BarcodeManagementView>("Barcode Management");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Barcode Management: {ex.Message}";
                _logger.LogError(ex, "Error navigating to Barcode Management");
            }
        }

        /// <summary>
        /// Navigates to the Import/Export view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ImportExport(object parameter)
        {
            try
            {
                NavigateToView<ProductImportExportView>("Import/Export");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Import/Export: {ex.Message}";
                _logger.LogError(ex, "Error navigating to Import/Export");
            }
        }

        /// <summary>
        /// Generic method to navigate to a view
        /// </summary>
        /// <typeparam name="T">Type of view to navigate to</typeparam>
        /// <param name="viewName">Name of the view for error messages</param>
        private void NavigateToView<T>(string viewName) where T : UserControl
        {
            var view = _serviceProvider.GetService<T>();
            if (view != null)
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var contentControl = mainWindow.FindName("MainContent") as ContentControl;
                    if (contentControl != null)
                    {
                        contentControl.Content = view;
                        return;
                    }
                }
            }

            MessageBox.Show($"Navigation to {viewName} view not implemented yet.",
                           "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
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