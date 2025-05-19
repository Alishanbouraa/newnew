// OfflinePOS.Admin/ViewModels/SupplierPaymentViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for processing payments to suppliers
    /// </summary>
    public class SupplierPaymentViewModel : ViewModelCommandBase
    {
        private readonly ISupplierInvoiceService _supplierInvoiceService;
        private readonly User _currentUser;
        private readonly Supplier _supplier;
        private readonly SupplierInvoice _invoice;

        private string _supplierName;
        private string _invoiceNumber;
        private decimal _currentBalance;
        private decimal _paymentAmount;
        private string _selectedPaymentMethod;
        private string _reference;
        private string _notes;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Supplier name
        /// </summary>
        public string SupplierName
        {
            get => _supplierName;
            set => SetProperty(ref _supplierName, value);
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
        /// Current balance for the supplier or invoice
        /// </summary>
        public decimal CurrentBalance
        {
            get => _currentBalance;
            set => SetProperty(ref _currentBalance, value);
        }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        /// <summary>
        /// Selected payment method
        /// </summary>
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        /// <summary>
        /// Reference information for the payment
        /// </summary>
        public string Reference
        {
            get => _reference;
            set => SetProperty(ref _reference, value);
        }

        /// <summary>
        /// Notes about the payment
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
        /// Collection of available payment methods
        /// </summary>
        public ObservableCollection<string> PaymentMethods { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Command for processing the payment
        /// </summary>
        public ICommand ProcessPaymentCommand { get; }

        /// <summary>
        /// Command for canceling the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the SupplierPaymentViewModel class for a specific invoice
        /// </summary>
        public SupplierPaymentViewModel(
            ISupplierInvoiceService supplierInvoiceService,
            ILogger<SupplierPaymentViewModel> logger,
            User currentUser,
            Supplier supplier,
            SupplierInvoice invoice = null)
            : base(logger)
        {
            _supplierInvoiceService = supplierInvoiceService ?? throw new ArgumentNullException(nameof(supplierInvoiceService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
            _invoice = invoice; // Can be null for general supplier payment

            // Initialize data from supplier/invoice
            SupplierName = supplier.Name;

            if (invoice != null)
            {
                InvoiceNumber = invoice.InvoiceNumber;
                CurrentBalance = invoice.RemainingBalance;

                // Default to pay full balance of the invoice
                PaymentAmount = CurrentBalance;
            }
            else
            {
                InvoiceNumber = "General Payment";
                CurrentBalance = supplier.CurrentBalance;

                // For general payments, don't set a default amount
                PaymentAmount = 0;
            }

            // Initialize payment methods
            PaymentMethods.Add("Cash");
            PaymentMethods.Add("Bank Transfer");
            PaymentMethods.Add("Check");
            PaymentMethods.Add("Credit Card");

            // Set default payment method
            SelectedPaymentMethod = "Bank Transfer";

            // Initialize commands
            ProcessPaymentCommand = CreateCommand(ProcessPayment, CanProcessPayment);
            CancelCommand = CreateCommand(Cancel);

            // Set up property changed handler
            PropertyChanged += SupplierPaymentViewModel_PropertyChanged;
        }

        /// <summary>
        /// Handles property changes to update dependent properties
        /// </summary>
        private void SupplierPaymentViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check if the payment amount or payment method changed
            if (e.PropertyName == nameof(PaymentAmount) || e.PropertyName == nameof(SelectedPaymentMethod))
            {
                // Refresh command availability
                (ProcessPaymentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Processes the payment
        /// </summary>
        private async void ProcessPayment(object parameter)
        {
            if (!ValidatePayment())
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Processing payment...";

                // Create the payment object
                var payment = new SupplierPayment
                {
                    SupplierId = _supplier.Id,
                    InvoiceId = _invoice?.Id, // Nullable - may be general payment
                    Amount = PaymentAmount,
                    PaymentMethod = SelectedPaymentMethod,
                    Reference = Reference,
                    Notes = Notes,
                    PaymentDate = DateTime.Now,
                    ProcessedById = _currentUser.Id,
                    CreatedById = _currentUser.Id,
                    IsActive = true
                };

                // Process the payment
                await _supplierInvoiceService.ProcessPaymentAsync(payment);

                StatusMessage = "Payment processed successfully";

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error processing payment: {ex.Message}";
                _logger.LogError(ex, "Error processing payment for supplier {SupplierId}", _supplier.Id);
                IsBusy = false;
            }
        }

        /// <summary>
        /// Validates the payment data
        /// </summary>
        private bool ValidatePayment()
        {
            ErrorMessage = string.Empty;

            if (PaymentAmount <= 0)
            {
                ErrorMessage = "Payment amount must be greater than zero";
                return false;
            }

            if (_invoice != null && PaymentAmount > CurrentBalance)
            {
                ErrorMessage = "Payment amount cannot exceed remaining balance for this invoice";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedPaymentMethod))
            {
                ErrorMessage = "Payment method is required";
                return false;
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
        /// Determines if payment can be processed
        /// </summary>
        private bool CanProcessPayment(object parameter)
        {
            return PaymentAmount > 0 &&
                   (_invoice == null || PaymentAmount <= CurrentBalance) &&
                   !string.IsNullOrWhiteSpace(SelectedPaymentMethod) &&
                   !IsBusy;
        }
    }
}