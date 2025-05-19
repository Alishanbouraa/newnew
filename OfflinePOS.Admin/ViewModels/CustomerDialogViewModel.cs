// OfflinePOS.Admin/ViewModels/CustomerDialogViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    public class CustomerDialogViewModel : ViewModelCommandBase
    {
        private readonly ICustomerService _customerService;
        private readonly User _currentUser;

        private Customer _customer;
        private bool _isNewCustomer;
        private string _windowTitle;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Customer being added or edited
        /// </summary>
        public Customer Customer
        {
            get => _customer;
            set => SetProperty(ref _customer, value);
        }

        /// <summary>
        /// Flag indicating if this is a new customer
        /// </summary>
        public bool IsNewCustomer
        {
            get => _isNewCustomer;
            set => SetProperty(ref _isNewCustomer, value);
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
        /// Command for saving the customer
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Command for canceling the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the CustomerDialogViewModel class
        /// </summary>
        /// <param name="customerService">Customer service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="customer">Customer to edit, or null for a new customer</param>
        public CustomerDialogViewModel(
            ICustomerService customerService,
            ILogger<CustomerDialogViewModel> logger,
            User currentUser,
            Customer customer = null)
            : base(logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Determine if adding or editing
            IsNewCustomer = customer == null;

            // Set window title
            WindowTitle = IsNewCustomer ? "Add New Customer" : "Edit Customer";

            // Initialize customer
            if (IsNewCustomer)
            {
                Customer = new Customer
                {
                    IsActive = true,
                    CreatedById = _currentUser.Id,
                    CurrentBalance = 0
                };
            }
            else
            {
                // Clone the customer to avoid modifying the original until Save
                Customer = CloneCustomer(customer);
            }

            // Initialize commands
            SaveCommand = CreateCommand(SaveCustomer, CanSaveCustomer);
            CancelCommand = CreateCommand(Cancel);
        }

        /// <summary>
        /// Saves the customer
        /// </summary>
        private async void SaveCustomer(object parameter)
        {
            if (!ValidateCustomer())
                return;

            try
            {
                IsBusy = true;

                if (IsNewCustomer)
                {
                    StatusMessage = "Creating customer...";
                    Customer.CreatedById = _currentUser.Id;
                    Customer.CreatedDate = DateTime.Now;
                    await _customerService.CreateCustomerAsync(Customer);
                }
                else
                {
                    StatusMessage = "Updating customer...";
                    Customer.LastUpdatedById = _currentUser.Id;
                    Customer.LastUpdatedDate = DateTime.Now;
                    await _customerService.UpdateCustomerAsync(Customer);
                }

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving customer: {ex.Message}";
                _logger.LogError(ex, "Error saving customer {CustomerId}", Customer.Id);
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Validates the customer data
        /// </summary>
        private bool ValidateCustomer()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Customer.Name))
            {
                ErrorMessage = "Customer name is required";
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
        /// Determines if the customer can be saved
        /// </summary>
        private bool CanSaveCustomer(object parameter)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Customer?.Name);
        }

        /// <summary>
        /// Creates a clone of a customer to avoid modifying the original
        /// </summary>
        private Customer CloneCustomer(Customer source)
        {
            if (source == null)
                return null;

            return new Customer
            {
                Id = source.Id,
                Name = source.Name,
                PhoneNumber = source.PhoneNumber,
                Address = source.Address,
                CurrentBalance = source.CurrentBalance,
                CreatedById = source.CreatedById,
                CreatedDate = source.CreatedDate,
                IsActive = source.IsActive
            };
        }
    }
}