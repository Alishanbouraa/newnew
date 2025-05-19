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
    /// Simplified ViewModel for creating or editing supplier invoices
    /// </summary>
    public class SupplierInvoiceDialogViewModel : ViewModelCommandBase
    {
        private readonly ISupplierInvoiceService _supplierInvoiceService;
        private readonly ISupplierService _supplierService;
        private readonly User _currentUser;
        private readonly bool _isNewInvoice;

        private string _windowTitle;
        private Supplier _supplier;
        private ObservableCollection<Supplier> _suppliers;
        private Supplier _selectedSupplier;
        private bool _isSupplierSelectable;
        private SupplierInvoice _invoice;
        private string _invoiceNumber;
        private DateTime _invoiceDate;
        private DateTime? _dueDate;
        private decimal _totalAmount;
        private string _notes;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;

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
        /// Collection of available suppliers
        /// </summary>
        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        /// <summary>
        /// Selected supplier
        /// </summary>
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (SetProperty(ref _selectedSupplier, value) && value != null)
                {
                    Supplier = value;
                }
            }
        }

        /// <summary>
        /// Flag indicating if supplier can be selected (depends on whether Supplier is already provided)
        /// </summary>
        public bool IsSupplierSelectable
        {
            get => _isSupplierSelectable;
            set => SetProperty(ref _isSupplierSelectable, value);
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
        /// Total amount of the invoice
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
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
            ISupplierService supplierService,
            ILogger<SupplierInvoiceDialogViewModel> logger,
            User currentUser,
            Supplier supplier = null,
            SupplierInvoice invoice = null)
            : base(logger)
        {
            _supplierInvoiceService = supplierInvoiceService ?? throw new ArgumentNullException(nameof(supplierInvoiceService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Initialize Supplier collection
            Suppliers = new ObservableCollection<Supplier>();

            // Determine if we're editing or creating, and if supplier is already selected
            _isNewInvoice = invoice == null;
            Supplier = supplier;
            IsSupplierSelectable = supplier == null;

            // Initialize the invoice
            if (_isNewInvoice)
            {
                _invoice = new SupplierInvoice
                {
                    InvoiceDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(30),
                    Status = "Pending",
                    CreatedById = _currentUser.Id,
                    IsActive = true
                };

                if (Supplier != null)
                {
                    _invoice.SupplierId = Supplier.Id;
                }

                // Generate a new invoice number 
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                InvoiceDate = _invoice.InvoiceDate;
                DueDate = _invoice.DueDate;
            }
            else
            {
                _invoice = invoice;
                InvoiceNumber = invoice.InvoiceNumber;
                InvoiceDate = invoice.InvoiceDate;
                DueDate = invoice.DueDate;
                TotalAmount = invoice.TotalAmount;
                Notes = invoice.Notes;
            }

            // Set window title
            WindowTitle = _isNewInvoice ? "Create New Supplier Invoice" : "Edit Supplier Invoice";

            // Initialize commands
            SaveInvoiceCommand = CreateCommand(SaveInvoice, CanSaveInvoice);
            CancelCommand = CreateCommand(Cancel);
        }

        /// <summary>
        /// Loads required data
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (IsSupplierSelectable)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = "Loading suppliers...";

                    // Load suppliers
                    var suppliers = await _supplierService.GetAllSuppliersAsync();
                    Suppliers.Clear();
                    foreach (var supplier in suppliers)
                    {
                        Suppliers.Add(supplier);
                    }

                    // If we have a supplier already, select it
                    if (Supplier != null)
                    {
                        SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == Supplier.Id);
                    }

                    StatusMessage = "Suppliers loaded successfully";
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error loading suppliers: {ex.Message}";
                    _logger.LogError(ex, "Error loading suppliers for invoice dialog");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Validates the invoice
        /// </summary>
        private bool ValidateInvoice()
        {
            ErrorMessage = string.Empty;

            if (Supplier == null)
            {
                ErrorMessage = "Supplier is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                ErrorMessage = "Invoice number is required";
                return false;
            }

            if (TotalAmount <= 0)
            {
                ErrorMessage = "Total amount must be greater than zero";
                return false;
            }

            return true;
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
                _invoice.SupplierId = Supplier.Id;
                _invoice.InvoiceNumber = InvoiceNumber;
                _invoice.InvoiceDate = InvoiceDate;
                _invoice.DueDate = DueDate;
                _invoice.TotalAmount = TotalAmount;
                _invoice.RemainingBalance = TotalAmount; // Initially, remaining balance = total
                _invoice.PaidAmount = 0; // Initially no payment
                _invoice.Notes = Notes;
                _invoice.LastUpdatedById = _currentUser.Id;
                _invoice.LastUpdatedDate = DateTime.Now;

                if (_isNewInvoice)
                {
                    // Create the invoice
                    var result = await _supplierInvoiceService.CreateInvoiceAsync(_invoice);
                    StatusMessage = $"Invoice {result.InvoiceNumber} created successfully";
                }
                else
                {
                    // Update the invoice
                    await _supplierInvoiceService.UpdateInvoiceAsync(_invoice);
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
        /// Cancels the operation
        /// </summary>
        private void Cancel(object parameter)
        {
            CloseRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Determines if the invoice can be saved
        /// </summary>
        private bool CanSaveInvoice(object parameter)
        {
            return (Supplier != null || SelectedSupplier != null) &&
                   !string.IsNullOrWhiteSpace(InvoiceNumber) &&
                   TotalAmount > 0 &&
                   !IsBusy;
        }
    }
}