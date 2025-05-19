using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using OfflinePOS.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for the Product Dialog (Add/Edit)
    /// </summary>
    public class ProductDialogViewModel : ViewModelCommandBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ISupplierService _supplierService;
        private readonly ISupplierInvoiceService _supplierInvoiceService;
        private readonly User _currentUser;

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

                    // Set the flag that enables/disables the invoice dropdown
                    HasSelectedSupplier = value != null && value.Id > 0;

                    // Clear and reload supplier invoices when supplier changes
                    if (HasSelectedSupplier)
                    {
                        LoadSupplierInvoices(null);
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

        /// <summary>
        /// Initializes a new instance of the ProductDialogViewModel class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="categoryService">Category service</param>
        /// <param name="supplierService">Supplier service</param>
        /// <param name="supplierInvoiceService">Supplier invoice service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="product">Product to edit, or null for a new product</param>
        public ProductDialogViewModel(
            IProductService productService,
            ICategoryService categoryService,
            ISupplierService supplierService,
            ISupplierInvoiceService supplierInvoiceService,
            ILogger<ProductDialogViewModel> logger,
            User currentUser,
            Product product = null)
            : base(logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _supplierInvoiceService = supplierInvoiceService ?? throw new ArgumentNullException(nameof(supplierInvoiceService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Determine if adding or editing
            IsNewProduct = product == null;

            // Set window title
            WindowTitle = IsNewProduct ? "Add New Product" : "Edit Product";

            // Initialize collections
            SupplierInvoices = new ObservableCollection<SupplierInvoice>();

            // Initialize product
            if (IsNewProduct)
            {
                Product = new Product
                {
                    ItemsPerBox = 1,
                    TrackInventory = true,
                    AllowNegativeInventory = false,
                    CreatedById = _currentUser.Id
                    // Description and Dimensions are now optional, so we don't need to set defaults
                };
            }
            else
            {
                // Make a copy of the product to avoid modifying the original until Save
                Product = CloneProduct(product);
            }

            // Initialize commands
            SaveCommand = CreateCommand(SaveProduct, CanSaveProduct);
            CancelCommand = CreateCommand(Cancel);
            GenerateBoxBarcodeCommand = CreateCommand(GenerateBoxBarcode);
            GenerateItemBarcodeCommand = CreateCommand(GenerateItemBarcode);
            RefreshInvoicesCommand = CreateCommand(LoadSupplierInvoices, CanLoadSupplierInvoices);
        }

        /// <summary>
        /// Loads categories, suppliers, and other necessary data
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading data...";

                // Load categories
                var categories = await _categoryService.GetCategoriesByTypeAsync("Product");
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Load suppliers
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                Suppliers.Clear();

                // Add a null option for supplier
                Suppliers.Add(new Supplier { Id = 0, Name = "-- None --" });

                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }

                // Set selected category and supplier based on product
                if (Product.CategoryId > 0)
                {
                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == Product.CategoryId);
                }
                else if (Categories.Count > 0)
                {
                    SelectedCategory = Categories.First();
                }

                if (Product.SupplierId.HasValue && Product.SupplierId.Value > 0)
                {
                    SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == Product.SupplierId.Value);
                    HasSelectedSupplier = true;

                    // Load supplier invoices if supplier is selected
                    if (HasSelectedSupplier)
                    {
                        await LoadSupplierInvoicesAsync();

                        // Select existing supplier invoice if available
                        if (Product.SupplierInvoiceId.HasValue)
                        {
                            SelectedSupplierInvoice = SupplierInvoices.FirstOrDefault(i => i.Id == Product.SupplierInvoiceId.Value);
                        }
                    }
                }
                else
                {
                    SelectedSupplier = Suppliers.First(); // The "None" option
                    HasSelectedSupplier = false;
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
        /// Loads supplier invoices for the selected supplier
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void LoadSupplierInvoices(object parameter)
        {
            if (!HasSelectedSupplier)
                return;

            await LoadSupplierInvoicesAsync();
        }

        /// <summary>
        /// Asynchronously loads supplier invoices
        /// </summary>
        private async Task LoadSupplierInvoicesAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading supplier invoices...";

                // Get invoices for the selected supplier
                var invoices = await _supplierInvoiceService.GetInvoicesBySupplierAsync(SelectedSupplier.Id);

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
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading supplier invoices: {ex.Message}";
                _logger.LogError(ex, "Error loading supplier invoices for supplier {SupplierId}", SelectedSupplier.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Determines if supplier invoices can be loaded
        /// </summary>
        private bool CanLoadSupplierInvoices(object parameter)
        {
            return HasSelectedSupplier && !IsBusy;
        }

        /// <summary>
        /// Saves the product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void SaveProduct(object parameter)
        {
            if (!ValidateProduct())
                return;

            try
            {
                IsBusy = true;

                // Add supplier invoice reference if selected
                if (SelectedSupplierInvoice != null)
                {
                    Product.SupplierInvoiceId = SelectedSupplierInvoice.Id;
                }
                else
                {
                    Product.SupplierInvoiceId = null;
                }

                if (IsNewProduct)
                {
                    StatusMessage = "Creating product...";
                    Product.CreatedById = _currentUser.Id;
                    var createdProduct = await _productService.CreateProductAsync(Product);

                    // Create initial stock if specified
                    if (Product.TrackInventory && (InitialBoxQuantity > 0 || InitialItemQuantity > 0))
                    {
                        // Get the stock service and update stock
                        var stockService = await _productService.GetStockServiceAsync();
                        if (stockService != null)
                        {
                            try
                            {
                                // Pass the location code with the stock adjustment
                                await stockService.UpdateStockLevelsAsync(
                                    createdProduct.Id,
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
                                _logger.LogError(ex, "Error setting initial stock for product {ProductId}", createdProduct.Id);
                            }
                        }
                    }
                }
                else
                {
                    StatusMessage = "Updating product...";
                    Product.LastUpdatedById = _currentUser.Id;
                    Product.LastUpdatedDate = DateTime.Now;
                    await _productService.UpdateProductAsync(Product);
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
        /// Validates the product data
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        private bool ValidateProduct()
        {
            // Clear previous error
            ErrorMessage = string.Empty;

            // Check required fields
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

            // Only require location code for new products with inventory tracking
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
        /// Generates a box barcode for the product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void GenerateBoxBarcode(object parameter)
        {
            try
            {
                // Generate a unique barcode using the BarcodeUtility
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
        /// Generates an item barcode for the product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void GenerateItemBarcode(object parameter)
        {
            try
            {
                // Generate a unique barcode using the BarcodeUtility
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
        /// Creates a clone of a product to avoid modifying the original
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
                SupplierProductCode = source.SupplierProductCode,
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