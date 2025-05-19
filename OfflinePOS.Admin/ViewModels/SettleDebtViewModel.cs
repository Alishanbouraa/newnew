// OfflinePOS.Admin/ViewModels/SettleDebtViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    public class SettleDebtViewModel : ViewModelCommandBase
    {
        private readonly ICustomerService _customerService;
        private readonly User _currentUser;
        private readonly Customer _customer;

        private string _customerName;
        private decimal _currentBalance;
        private decimal _paymentAmount;
        private decimal _newBalance;
        private string _selectedPaymentMethod;
        private string _reference;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Customer name
        /// </summary>
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        /// <summary>
        /// Current balance for the customer
        /// </summary>
        public decimal CurrentBalance
        {
            get => _currentBalance;
            set => SetProperty(ref _currentBalance, value);
        }

        /// <summary>
        /// Payment amount to be applied
        /// </summary>
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (SetProperty(ref _paymentAmount, value))
                {
                    // Update the new balance projection
                    CalculateNewBalance();
                }
            }
        }

        /// <summary>
        /// Projected new balance after payment
        /// </summary>
        public decimal NewBalance
        {
            get => _newBalance;
            set => SetProperty(ref _newBalance, value);
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
        /// Payment reference
        /// </summary>
        public string Reference
        {
            get => _reference;
            set => SetProperty(ref _reference, value);
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
        /// Initializes a new instance of the SettleDebtViewModel class
        /// </summary>
        /// <param name="customerService">Customer service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="customer">Customer entity</param>
        public SettleDebtViewModel(
            ICustomerService customerService,
            ILogger<SettleDebtViewModel> logger,
            User currentUser,
            Customer customer)
            : base(logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _customer = customer ?? throw new ArgumentNullException(nameof(customer));

            // Initialize data from customer
            CustomerName = customer.Name;
            CurrentBalance = customer.CurrentBalance;

            // Default to pay full balance
            PaymentAmount = CurrentBalance;

            // Initialize payment methods
            PaymentMethods.Add("Cash");
            PaymentMethods.Add("Card");
            PaymentMethods.Add("Check");
            PaymentMethods.Add("Bank Transfer");

            // Set default payment method
            SelectedPaymentMethod = "Cash";

            // Calculate initial new balance
            CalculateNewBalance();

            // Initialize commands
            ProcessPaymentCommand = CreateCommand(ProcessPayment, CanProcessPayment);
            CancelCommand = CreateCommand(Cancel);

            // Set up property changed handler
            PropertyChanged += SettleDebtViewModel_PropertyChanged;
        }

        /// <summary>
        /// Handles property changes to update dependent properties
        /// </summary>
        private void SettleDebtViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaymentAmount) || e.PropertyName == nameof(CurrentBalance))
            {
                // Recalculate the new balance
                CalculateNewBalance();
            }
        }

        /// <summary>
        /// Calculates the new balance after payment
        /// </summary>
        private void CalculateNewBalance()
        {
            NewBalance = Math.Max(0, CurrentBalance - PaymentAmount);
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

                await _customerService.SettleDebtAsync(
                    _customer.Id,
                    PaymentAmount,
                    SelectedPaymentMethod,
                    Reference,
                    _currentUser.Id);

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error processing payment: {ex.Message}";
                _logger.LogError(ex, "Error processing payment for customer {CustomerId}", _customer.Id);
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
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

            if (PaymentAmount > CurrentBalance)
            {
                ErrorMessage = "Payment amount cannot exceed current balance";
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
        /// Cancels the operation and closes the dialog
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
            return !IsBusy &&
                   PaymentAmount > 0 &&
                   PaymentAmount <= CurrentBalance &&
                   !string.IsNullOrWhiteSpace(SelectedPaymentMethod);
        }
    }
}