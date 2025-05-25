// File: OfflinePOS.Admin/ViewModels/ProductDialogViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using OfflinePOS.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for the Product Dialog (Add/Edit) with proper service scoping to prevent DbContext concurrency issues
    /// </summary>
    public class ProductDialogViewModel : ViewModelCommandBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly User _currentUser;

        // UI state properties
        private Product _product;
        private Category _selectedCategory;
        private Supplier _selectedSupplier;
        private ObservableCollection<SupplierInvoice> _supplierInvoices;
        private SupplierInvoice _selectedSupplierInvoice;
        private bool _hasSelectedSupplier;
        private bool _isNewProduct;
        private string _windowTitle;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;
        private int _initialBoxQuantity;
        private int _initialItemQuantity;
        private string _locationCode = string.Empty;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        #region Properties

        /// <summary>
        /// Product being added or edited
        /// </summary>
        public Product Product
        {
            get => _product;
            set => SetProperty(ref _product, value);
        }

        /// <summary>
        /// Selected category for the product
        /// </summary>
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && _product != null && value != null)
                {
                    _product.CategoryId = value.Id;
                }
            }
        }

        /// <summary>
        /// Selected supplier for the product
        /// </summary>
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (SetProperty(ref _selectedSupplier, value))
                {
                    if (_product != null)
                    {
                        _product.SupplierId = value?.Id > 0 ? value.Id : null;
                    }

                    HasSelectedSupplier = value != null && value.Id > 0;

                    if (HasSelectedSupplier)
                    {
                        _ = LoadSupplierInvoicesAsync();
                    }
                    else
                    {
                        SupplierInvoices.Clear();
                        SelectedSupplierInvoice = null;
                    }
                }
            }
        }

        /// <summary>
        /// Available supplier invoices
        /// </summary>
        public ObservableCollection<SupplierInvoice> SupplierInvoices
        {
            get => _supplierInvoices;
            set => SetProperty(ref _supplierInvoices, value);
        }

        /// <summary>
        /// Selected supplier invoice
        /// </summary>
        public SupplierInvoice SelectedSupplierInvoice
        {
            get => _selectedSupplierInvoice;
            set => SetProperty(ref _selectedSupplierInvoice, value);
        }

        /// <summary>
        /// Flag indicating if a supplier is selected
        /// </summary>
        public bool HasSelectedSupplier
        {
            get => _hasSelectedSupplier;
            set => SetProperty(ref _hasSelectedSupplier, value);
        }

        /// <summary>
        /// Flag indicating if this is a new product
        /// </summary>
        public bool IsNewProduct
        {
            get => _isNewProduct;
            set => SetProperty(ref _isNewProduct, value);
        }

        /// <summary>
        /// Title of the dialog window
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Error message to display
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Status message to display during busy operations
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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
        /// Location code for initial stock setup
        /// </summary>
        public string LocationCode
        {
            get => _locationCode;
            set => SetProperty(ref _locationCode, value);
        }

        /// <summary>
        /// Initial box quantity for new products
        /// </summary>
        public int InitialBoxQuantity
        {
            get => _initialBoxQuantity;
            set => SetProperty(ref _initialBoxQuantity, value);
        }

        /// <summary>
        /// Initial item quantity for new products
        /// </summary>
        public int InitialItemQuantity
        {
            get => _initialItemQuantity;
            set => SetProperty(ref _initialItemQuantity, value);
        }

        /// <summary>
        /// Collection of available categories
        /// </summary>
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        /// <summary>
        /// Collection of available suppliers
        /// </summary>
        public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();

        #endregion

        #region Commands

        /// <summary>
        /// Command for saving the product
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Command for canceling the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Command for generating a box barcode
        /// </summary>
        public ICommand GenerateBoxBarcodeCommand { get; }

        /// <summary>
        /// Command for generating an item barcode
        /// </summary>
        public ICommand GenerateItemBarcodeCommand { get; }

        /// <summary>
        /// Command for refreshing supplier invoices
        /// </summary>
        public ICommand RefreshInvoicesCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the ProductDialogViewModel class
        /// </summary>
        /// <param name="serviceProvider">Service provider for creating scoped services</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="product">Product to edit, or null for a new product</param>
        public ProductDialogViewModel(
            IServiceProvider serviceProvider,
            ILogger<ProductDialogViewModel> logger,
            User currentUser,
            Product product = null)
            : base(logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Determine if adding or editing
            IsNewProduct = product == null;
            WindowTitle = IsNewProduct ? "Add New Product" : "Edit Product";

            // Initialize collections
            SupplierInvoices = new ObservableCollection<SupplierInvoice>();

            // Initialize product
            InitializeProduct(product);

            // Initialize commands
            SaveCommand = CreateCommand(SaveProductAsync, CanSaveProduct);
            CancelCommand = CreateCommand(Cancel);
            GenerateBoxBarcodeCommand = CreateCommand(GenerateBoxBarcode);
            GenerateItemBarcodeCommand = CreateCommand(GenerateItemBarcode);
            RefreshInvoicesCommand = CreateCommand(LoadSupplierInvoicesCommand, CanLoadSupplierInvoices);
        }

        /// <summary>
        /// Initializes the product instance
        /// </summary>
        /// <param name="product">Existing product or null for new product</param>
        private void InitializeProduct(Product product)
        {
            if (IsNewProduct)
            {
                Product = new Product
                {
                    ItemsPerBox = 1,
                    TrackInventory = true,
                    AllowNegativeInventory = false,
                    CreatedById = _currentUser.Id,
                    SupplierProductCode = string.Empty
                };
            }
            else
            {
                Product = CloneProduct(product);
            }
        }

        /// <summary>
        /// Loads categories, suppliers, and other necessary data using proper service scoping
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading data...";

                // Create a dedicated service scope for this operation
                using (var scope = _serviceProvider.CreateScope())
                {
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                    var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();

                    // Load categories
                    await LoadCategoriesAsync(categoryService);

                    // Load suppliers
                    await LoadSuppliersAsync(supplierService);

                    // Set selected values based on product data
                    SetSelectedValues();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading data: {ex.Message}";
                _logger.LogError(ex, "Error loading data for product dialog");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Loads categories from the database
        /// </summary>
        /// <param name="categoryService">Category service instance</param>
        private async Task LoadCategoriesAsync(ICategoryService categoryService)
        {
            var categories = await categoryService.GetCategoriesByTypeAsync("Product");
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }

        /// <summary>
        /// Loads suppliers from the database
        /// </summary>
        /// <param name="supplierService">Supplier service instance</param>
        private async Task LoadSuppliersAsync(ISupplierService supplierService)
        {
            var suppliers = await supplierService.GetAllSuppliersAsync();
            Suppliers.Clear();

            // Add a null option for supplier
            Suppliers.Add(new Supplier { Id = 0, Name = "-- None --" });

            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }

        /// <summary>
        /// Sets selected category and supplier based on product data
        /// </summary>
        private void SetSelectedValues()
        {
            // Set selected category
            if (Product.CategoryId > 0)
            {
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == Product.CategoryId);
            }
            else if (Categories.Count > 0)
            {
                SelectedCategory = Categories.First();
                if (Product != null)
                {
                    Product.CategoryId = SelectedCategory.Id;
                }
            }

            // Set selected supplier
            if (Product.SupplierId.HasValue && Product.SupplierId.Value > 0)
            {
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == Product.SupplierId.Value);
                HasSelectedSupplier = true;

                // Load supplier invoices if supplier is selected
                if (HasSelectedSupplier)
                {
                    _ = LoadSupplierInvoicesAsync();
                }
            }
            else
            {
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == 0); // The "None" option
                HasSelectedSupplier = false;
            }
        }

        /// <summary>
        /// Loads supplier invoices for the selected supplier using proper service scoping
        /// </summary>
        private async Task LoadSupplierInvoicesAsync()
        {
            if (!HasSelectedSupplier || SelectedSupplier?.Id <= 0)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading supplier invoices...";

                // Create a dedicated service scope for this operation
                using (var scope = _serviceProvider.CreateScope())
                {
                    var supplierInvoiceService = scope.ServiceProvider.GetRequiredService<ISupplierInvoiceService>();

                    // Get invoices for the selected supplier
                    var invoices = await supplierInvoiceService.GetInvoicesBySupplierAsync(SelectedSupplier.Id);

                    // Filter to only pending/partially paid invoices
                    var activeInvoices = invoices.Where(i =>
                        i.Status == "Pending" || i.Status == "PartiallyPaid");

                    SupplierInvoices.Clear();
                    foreach (var invoice in activeInvoices)
                    {
                        SupplierInvoices.Add(invoice);
                    }

                    // If editing a product that has a supplier invoice, select it
                    if (Product?.SupplierInvoiceId != null)
                    {
                        SelectedSupplierInvoice = SupplierInvoices
                            .FirstOrDefault(i => i.Id == Product.SupplierInvoiceId);
                    }

                    StatusMessage = $"Loaded {SupplierInvoices.Count} supplier invoices";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading supplier invoices: {ex.Message}";
                _logger.LogError(ex, "Error loading supplier invoices for supplier {SupplierId}", SelectedSupplier?.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Command handler for loading supplier invoices
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void LoadSupplierInvoicesCommand(object parameter)
        {
            _ = LoadSupplierInvoicesAsync();
        }

        /// <summary>
        /// Saves the product using proper service scoping to prevent DbContext conflicts
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void SaveProductAsync(object parameter)
        {
            if (!ValidateProduct())
                return;

            try
            {
                IsBusy = true;

                // Set supplier invoice reference if selected
                if (SelectedSupplierInvoice != null)
                {
                    Product.SupplierInvoiceId = SelectedSupplierInvoice.Id;
                }
                else
                {
                    Product.SupplierInvoiceId = null;
                }

                // Create a dedicated service scope for the entire save operation
                using (var scope = _serviceProvider.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                    Product createdProduct = null;

                    if (IsNewProduct)
                    {
                        StatusMessage = "Creating product...";
                        Product.CreatedById = _currentUser.Id;
                        createdProduct = await productService.CreateProductAsync(Product);

                        // Create initial stock within the same scope to avoid DbContext conflicts
                        if (Product.TrackInventory && (InitialBoxQuantity > 0 || InitialItemQuantity > 0))
                        {
                            await CreateInitialStock(scope, createdProduct.Id);
                        }
                    }
                    else
                    {
                        StatusMessage = "Updating product...";
                        Product.LastUpdatedById = _currentUser.Id;
                        Product.LastUpdatedDate = DateTime.Now;
                        await productService.UpdateProductAsync(Product);
                    }
                }

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving product: {ex.Message}";
                _logger.LogError(ex, "Error saving product");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Creates initial stock for a new product
        /// </summary>
        /// <param name="scope">Service scope</param>
        /// <param name="productId">Product ID</param>
        private async Task CreateInitialStock(IServiceScope scope, int productId)
        {
            try
            {
                var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();

                await stockService.UpdateStockLevelsAsync(
                    productId,
                    InitialBoxQuantity,
                    InitialItemQuantity,
                    "Addition",
                    "Initial stock",
                    "INIT-STOCK",
                    _currentUser.Id,
                    LocationCode);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Product created but failed to set initial stock: {ex.Message}";
                _logger.LogError(ex, "Error setting initial stock for product {ProductId}", productId);
            }
        }

        /// <summary>
        /// Validates the product data before saving
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        private bool ValidateProduct()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Product.Name))
            {
                ErrorMessage = "Product name is required";
                return false;
            }

            if (SelectedCategory == null)
            {
                ErrorMessage = "Category is required";
                return false;
            }

            if (Product.ItemsPerBox <= 0)
            {
                ErrorMessage = "Items per box must be greater than zero";
                return false;
            }

            if (IsNewProduct && Product.TrackInventory && InitialBoxQuantity > 0 && string.IsNullOrWhiteSpace(LocationCode))
            {
                ErrorMessage = "Location code is required for inventory tracking";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cancels the operation and closes the dialog
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void Cancel(object parameter)
        {
            CloseRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Generates a unique box barcode for the product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void GenerateBoxBarcode(object parameter)
        {
            try
            {
                string barcode = BarcodeUtility.GenerateEAN13(DateTime.Now.Millisecond);
                Product.BoxBarcode = barcode;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error generating barcode: {ex.Message}";
                _logger.LogError(ex, "Error generating box barcode");
            }
        }

        /// <summary>
        /// Generates a unique item barcode for the product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void GenerateItemBarcode(object parameter)
        {
            try
            {
                string barcode = BarcodeUtility.GenerateEAN13(DateTime.Now.Millisecond + 1000);
                Product.ItemBarcode = barcode;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error generating barcode: {ex.Message}";
                _logger.LogError(ex, "Error generating item barcode");
            }
        }

        /// <summary>
        /// Determines if the product can be saved
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if the product can be saved, false otherwise</returns>
        private bool CanSaveProduct(object parameter)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Product?.Name);
        }

        /// <summary>
        /// Determines if supplier invoices can be loaded
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if invoices can be loaded, false otherwise</returns>
        private bool CanLoadSupplierInvoices(object parameter)
        {
            return HasSelectedSupplier && !IsBusy;
        }

        /// <summary>
        /// Creates a deep clone of a product to avoid modifying the original
        /// </summary>
        /// <param name="source">Source product</param>
        /// <returns>Cloned product</returns>
        private Product CloneProduct(Product source)
        {
            if (source == null)
                return null;

            return new Product
            {
                Id = source.Id,
                CategoryId = source.CategoryId,
                Name = source.Name,
                Description = source.Description,
                BoxBarcode = source.BoxBarcode,
                ItemBarcode = source.ItemBarcode,
                ItemsPerBox = source.ItemsPerBox,
                BoxPurchasePrice = source.BoxPurchasePrice,
                BoxWholesalePrice = source.BoxWholesalePrice,
                BoxSalePrice = source.BoxSalePrice,
                ItemPurchasePrice = source.ItemPurchasePrice,
                ItemWholesalePrice = source.ItemWholesalePrice,
                ItemSalePrice = source.ItemSalePrice,
                SupplierId = source.SupplierId,
                SupplierInvoiceId = source.SupplierInvoiceId,
                SupplierProductCode = source.SupplierProductCode ?? string.Empty,
                MSRP = source.MSRP,
                TrackInventory = source.TrackInventory,
                AllowNegativeInventory = source.AllowNegativeInventory,
                Weight = source.Weight,
                Dimensions = source.Dimensions,
                CreatedById = source.CreatedById,
                CreatedDate = source.CreatedDate,
                IsActive = source.IsActive
            };
        }
    }
}