// OfflinePOS.Admin/ViewModels/SupplierInvoiceDialogViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for creating or editing supplier invoices
    /// </summary>
    public class SupplierInvoiceDialogViewModel : ViewModelCommandBase
    {
        private readonly ISupplierInvoiceService _supplierInvoiceService;
        private readonly IProductService _productService;
        private readonly User _currentUser;
        private readonly bool _isNewInvoice;

        private string _windowTitle;
        private Supplier _supplier;
        private SupplierInvoice _invoice;
        private string _invoiceNumber;
        private DateTime _invoiceDate;
        private DateTime? _dueDate;
        private string _notes;
        private string _productSearchText;
        private Product _selectedProduct;
        private int _boxQuantity;
        private int _itemQuantity;
        private SupplierInvoiceItem _selectedInvoiceItem;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;
        private int _totalItems;
        private decimal _totalAmount;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Window title
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Supplier for the invoice
        /// </summary>
        public Supplier Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value);
        }

        /// <summary>
        /// Invoice number
        /// </summary>
        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        /// <summary>
        /// Invoice date
        /// </summary>
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }

        /// <summary>
        /// Due date
        /// </summary>
        public DateTime? DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
        }

        /// <summary>
        /// Notes about the invoice
        /// </summary>
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        /// <summary>
        /// Product search text
        /// </summary>
        public string ProductSearchText
        {
            get => _productSearchText;
            set => SetProperty(ref _productSearchText, value);
        }

        /// <summary>
        /// Selected product
        /// </summary>
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value) && value != null)
                {
                    // Default to 1 box when selecting a product
                    BoxQuantity = 1;
                    ItemQuantity = 0;
                }
            }
        }

        /// <summary>
        /// Box quantity
        /// </summary>
        public int BoxQuantity
        {
            get => _boxQuantity;
            set => SetProperty(ref _boxQuantity, value);
        }

        /// <summary>
        /// Item quantity
        /// </summary>
        public int ItemQuantity
        {
            get => _itemQuantity;
            set => SetProperty(ref _itemQuantity, value);
        }

        /// <summary>
        /// Selected invoice item
        /// </summary>
        public SupplierInvoiceItem SelectedInvoiceItem
        {
            get => _selectedInvoiceItem;
            set => SetProperty(ref _selectedInvoiceItem, value);
        }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Status message
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
        /// Total number of items in the invoice
        /// </summary>
        public int TotalItems
        {
            get => _totalItems;
            set => SetProperty(ref _totalItems, value);
        }

        /// <summary>
        /// Total amount of the invoice
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        /// <summary>
        /// Collection of products for searching
        /// </summary>
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        /// <summary>
        /// Collection of invoice items
        /// </summary>
        public ObservableCollection<SupplierInvoiceItem> InvoiceItems { get; } = new ObservableCollection<SupplierInvoiceItem>();

        /// <summary>
        /// Command for searching products
        /// </summary>
        public ICommand SearchProductsCommand { get; }

        /// <summary>
        /// Command for adding an item to the invoice
        /// </summary>
        public ICommand AddItemCommand { get; }

        /// <summary>
        /// Command for editing an invoice item
        /// </summary>
        public ICommand EditItemCommand { get; }

        /// <summary>
        /// Command for removing an invoice item
        /// </summary>
        public ICommand RemoveItemCommand { get; }

        /// <summary>
        /// Command for clearing the product selection
        /// </summary>
        public ICommand ClearSelectionCommand { get; }

        /// <summary>
        /// Command for saving the invoice
        /// </summary>
        public ICommand SaveInvoiceCommand { get; }

        /// <summary>
        /// Command for canceling the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceDialogViewModel class
        /// </summary>
        public SupplierInvoiceDialogViewModel(
            ISupplierInvoiceService supplierInvoiceService,
            IProductService productService,
            ILogger<SupplierInvoiceDialogViewModel> logger,
            User currentUser,
            Supplier supplier,
            SupplierInvoice invoice = null)
            : base(logger)
        {
            _supplierInvoiceService = supplierInvoiceService ?? throw new ArgumentNullException(nameof(supplierInvoiceService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            Supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

            // Determine if adding or editing
            _isNewInvoice = invoice == null;

            // Initialize the invoice
            if (_isNewInvoice)
            {
                _invoice = new SupplierInvoice
                {
                    SupplierId = Supplier.Id,
                    InvoiceDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(30),
                    Status = "Pending",
                    CreatedById = _currentUser.Id,
                    IsActive = true
                };

                // Generate a new invoice number (could be improved with a proper sequence)
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }
            else
            {
                _invoice = invoice;
                InvoiceNumber = invoice.InvoiceNumber;
                InvoiceDate = invoice.InvoiceDate;
                DueDate = invoice.DueDate;
                Notes = invoice.Notes;
            }

            // Set window title
            WindowTitle = _isNewInvoice ? "Create New Supplier Invoice" : "Edit Supplier Invoice";

            // Initialize collections
            Products = new ObservableCollection<Product>();
            InvoiceItems = new ObservableCollection<SupplierInvoiceItem>();

            // Set default dates
            InvoiceDate = _invoice.InvoiceDate;
            DueDate = _invoice.DueDate;

            // Initialize commands
            SearchProductsCommand = CreateCommand(SearchProducts);
            AddItemCommand = CreateCommand(AddItem, CanAddItem);
            EditItemCommand = CreateCommand(EditItem, CanEditItem);
            RemoveItemCommand = CreateCommand(RemoveItem, CanEditItem);
            ClearSelectionCommand = CreateCommand(ClearSelection);
            SaveInvoiceCommand = CreateCommand(SaveInvoice, CanSaveInvoice);
            CancelCommand = CreateCommand(Cancel);

            // Set up property changed handler
            PropertyChanged += SupplierInvoiceDialogViewModel_PropertyChanged;
        }

        /// <summary>
        /// Handles property changes to update dependent properties
        /// </summary>
        private void SupplierInvoiceDialogViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceItems))
            {
                UpdateTotals();
            }
        }

        /// <summary>
        /// Loads data for the dialog
        /// </summary>
        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading data...";

                // Load existing invoice items if editing
                if (!_isNewInvoice)
                {
                    var items = await _supplierInvoiceService.GetInvoiceItemsAsync(_invoice.Id);
                    InvoiceItems.Clear();
                    foreach (var item in items)
                    {
                        // Load the product for each item
                        item.Product = await _productService.GetProductByIdAsync(item.ProductId);
                        InvoiceItems.Add(item);
                    }
                }

                UpdateTotals();
                StatusMessage = "Data loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                _logger.LogError(ex, "Error loading data for supplier invoice dialog");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Updates total calculations
        /// </summary>
        private void UpdateTotals()
        {
            TotalItems = InvoiceItems.Count;
            TotalAmount = InvoiceItems.Sum(i => i.TotalAmount);
        }

        /// <summary>
        /// Searches for products
        /// </summary>
        private async void SearchProducts(object parameter)
        {
            if (string.IsNullOrWhiteSpace(ProductSearchText))
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Searching products...";

                var products = await _productService.SearchProductsAsync(ProductSearchText);

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                if (Products.Count > 0)
                {
                    SelectedProduct = Products[0]; // Auto-select first product
                    StatusMessage = $"Found {Products.Count} products - click an item to select it";
                }
                else
                {
                    SelectedProduct = null;
                    StatusMessage = "No products found matching your search";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching products: {ex.Message}";
                ErrorMessage = "Failed to search products. Please try again.";
                _logger.LogError(ex, "Error searching products with term: {SearchTerm}", ProductSearchText);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adds an item to the invoice
        /// </summary>
        private void AddItem(object parameter)
        {
            if (SelectedProduct == null)
                return;

            try
            {
                // Calculate total amount
                decimal totalAmount = (BoxQuantity * SelectedProduct.BoxPurchasePrice) +
                                     (ItemQuantity * SelectedProduct.ItemPurchasePrice);

                // Check if the product already exists in the invoice
                var existingItem = InvoiceItems.FirstOrDefault(i => i.ProductId == SelectedProduct.Id);
                if (existingItem != null)
                {
                    // Update existing item
                    existingItem.BoxQuantity += BoxQuantity;
                    existingItem.ItemQuantity += ItemQuantity;
                    existingItem.TotalAmount += totalAmount;

                    // Refresh the collection to update the UI
                    var index = InvoiceItems.IndexOf(existingItem);
                    InvoiceItems.Remove(existingItem);
                    InvoiceItems.Insert(index, existingItem);
                }
                else
                {
                    // Create a new item
                    var invoiceItem = new SupplierInvoiceItem
                    {
                        ProductId = SelectedProduct.Id,
                        BoxQuantity = BoxQuantity,
                        ItemQuantity = ItemQuantity,
                        BoxPurchasePrice = SelectedProduct.BoxPurchasePrice,
                        ItemPurchasePrice = SelectedProduct.ItemPurchasePrice,
                        TotalAmount = totalAmount,
                        Product = SelectedProduct,
                        CreatedById = _currentUser.Id,
                        IsActive = true
                    };

                    InvoiceItems.Add(invoiceItem);
                }

                // Update totals
                UpdateTotals();

                // Clear selection for next item
                ClearSelection(null);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error adding item: {ex.Message}";
                _logger.LogError(ex, "Error adding item to invoice");
            }
        }

        /// <summary>
        /// Edits an invoice item
        /// </summary>
        private void EditItem(object parameter)
        {
            var item = parameter as SupplierInvoiceItem ?? SelectedInvoiceItem;
            if (item == null)
                return;

            try
            {
                // Set the selected product and quantities for editing
                SelectedProduct = item.Product;
                BoxQuantity = item.BoxQuantity;
                ItemQuantity = item.ItemQuantity;

                // Remove the item from the list (it will be re-added when user clicks Add Item)
                InvoiceItems.Remove(item);

                // Update totals
                UpdateTotals();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error editing item: {ex.Message}";
                _logger.LogError(ex, "Error editing invoice item");
            }
        }

        /// <summary>
        /// Removes an invoice item
        /// </summary>
        private void RemoveItem(object parameter)
        {
            var item = parameter as SupplierInvoiceItem ?? SelectedInvoiceItem;
            if (item == null)
                return;

            try
            {
                InvoiceItems.Remove(item);
                UpdateTotals();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error removing item: {ex.Message}";
                _logger.LogError(ex, "Error removing invoice item");
            }
        }

        /// <summary>
        /// Clears the product selection
        /// </summary>
        private void ClearSelection(object parameter)
        {
            SelectedProduct = null;
            BoxQuantity = 0;
            ItemQuantity = 0;
            ProductSearchText = string.Empty;
            Products.Clear();
        }

        /// <summary>
        /// Saves the invoice
        /// </summary>
        private async void SaveInvoice(object parameter)
        {
            if (!ValidateInvoice())
                return;

            try
            {
                IsBusy = true;
                StatusMessage = _isNewInvoice ? "Creating invoice..." : "Updating invoice...";

                // Update invoice properties
                _invoice.InvoiceNumber = InvoiceNumber;
                _invoice.InvoiceDate = InvoiceDate;
                _invoice.DueDate = DueDate;
                _invoice.Notes = Notes;
                _invoice.TotalAmount = TotalAmount;
                _invoice.RemainingBalance = TotalAmount; // Initially, remaining balance = total
                _invoice.PaidAmount = 0; // Initially no payment
                _invoice.LastUpdatedById = _currentUser.Id;
                _invoice.LastUpdatedDate = DateTime.Now;

                // Create or update the invoice
                if (_isNewInvoice)
                {
                    // Add items to the invoice
                    _invoice.Items = InvoiceItems.ToList();

                    // Create the invoice
                    var result = await _supplierInvoiceService.CreateInvoiceAsync(_invoice);

                    StatusMessage = $"Invoice {result.InvoiceNumber} created successfully";
                }
                else
                {
                    // Update the invoice
                    await _supplierInvoiceService.UpdateInvoiceAsync(_invoice);

                    // TODO: Handle updating invoice items - would need to:
                    // 1. Get existing items
                    // 2. Compare with current items
                    // 3. Add new items, update existing, remove deleted

                    StatusMessage = $"Invoice {_invoice.InvoiceNumber} updated successfully";
                }

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving invoice: {ex.Message}";
                _logger.LogError(ex, "Error saving supplier invoice");
                IsBusy = false;
            }
        }

        /// <summary>
        /// Validates the invoice
        /// </summary>
        private bool ValidateInvoice()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                ErrorMessage = "Invoice number is required";
                return false;
            }

            if (InvoiceItems.Count == 0)
            {
                ErrorMessage = "Invoice must have at least one item";
                return false;
            }

            if (TotalAmount <= 0)
            {
                ErrorMessage = "Total amount must be greater than zero";
                return false;
            }

            // Validate each item has valid quantities
            foreach (var item in InvoiceItems)
            {
                if (item.BoxQuantity <= 0 && item.ItemQuantity <= 0)
                {
                    ErrorMessage = $"Item '{item.Product.Name}' must have a quantity greater than zero";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Cancels the operation
        /// </summary>
        private void Cancel(object parameter)
        {
            CloseRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Determines if an item can be added
        /// </summary>
        private bool CanAddItem(object parameter)
        {
            return SelectedProduct != null &&
                  (BoxQuantity > 0 || ItemQuantity > 0) &&
                  !IsBusy;
        }

        /// <summary>
        /// Determines if an item can be edited or removed
        /// </summary>
        private bool CanEditItem(object parameter)
        {
            return (parameter as SupplierInvoiceItem ?? SelectedInvoiceItem) != null &&
                   !IsBusy;
        }

        /// <summary>
        /// Determines if the invoice can be saved
        /// </summary>
        private bool CanSaveInvoice(object parameter)
        {
            return !string.IsNullOrWhiteSpace(InvoiceNumber) &&
                   InvoiceItems.Count > 0 &&
                   TotalAmount > 0 &&
                   !IsBusy;
        }
    }
}