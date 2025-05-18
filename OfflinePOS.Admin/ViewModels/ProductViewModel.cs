// File: OfflinePOS.Admin/ViewModels/ProductViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
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
    /// ViewModel for product management
    /// </summary>
    public class ProductViewModel : InventoryViewModelBase
    {
        private readonly ICategoryService _categoryService;
        private Product _selectedProduct;
        private Category _selectedCategory;
        private ObservableCollection<Category> _categories;

        /// <summary>
        /// Currently selected product
        /// </summary>
        public Product SelectedProduct
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
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        public ProductViewModel(
            IProductService productService,
            IStockService stockService,
            ICategoryService categoryService,
            ILogger<ProductViewModel> logger,
            User currentUser)
            : base(productService, stockService, logger, currentUser)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

            // Initialize collections
            Categories = new ObservableCollection<Category>();

            // Initialize commands
            AddProductCommand = CreateCommand(AddProduct);
            EditProductCommand = CreateCommand(EditProduct);
            DeleteProductCommand = CreateCommand(DeleteProduct);
            ManageStockCommand = CreateCommand(ManageStock);
            ManageBarcodesCommand = CreateCommand(ManageBarcodes);
            ImportExportCommand = CreateCommand(ImportExport);

            // Load categories
            LoadCategories();
        }

        /// <summary>
        /// Loads the list of categories
        /// </summary>
        private async void LoadCategories()
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
                _logger.LogError(ex, "Error searching products");
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
            // In a real implementation, you would open a dialog to add a new product
            MessageBox.Show("Add Product functionality not implemented yet.",
                           "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Opens the Edit Product dialog for the selected product
        /// </summary>
        /// <param name="parameter">Product to edit</param>
        private void EditProduct(object parameter)
        {
            // In a real implementation, you would open a dialog to edit the product
            var product = parameter as Product;
            if (product == null)
                return;

            MessageBox.Show($"Edit Product functionality not implemented yet for product: {product.Name}",
                           "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Deletes the selected product after confirmation
        /// </summary>
        /// <param name="parameter">Product to delete</param>
        private async void DeleteProduct(object parameter)
        {
            var product = parameter as Product;
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
            // In a real implementation, this would navigate to the Stock Management view
            MessageBox.Show("Navigation to Stock Management view not implemented yet.",
                           "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Opens the Barcode Management view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ManageBarcodes(object parameter)
        {
            // In a real implementation, this would navigate to the Barcode Management view
            MessageBox.Show("Navigation to Barcode Management view not implemented yet.",
                           "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Opens the Import/Export view
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ImportExport(object parameter)
        {
            // In a real implementation, this would navigate to the Import/Export view
            MessageBox.Show("Navigation to Import/Export view not implemented yet.",
                           "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}