// OfflinePOS.Admin/ViewModels/SupplierDialogViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    public class SupplierDialogViewModel : ViewModelCommandBase
    {
        private readonly ISupplierService _supplierService;
        private readonly User _currentUser;

        private Supplier _supplier;
        private bool _isNewSupplier;
        private string _windowTitle;
        private string _errorMessage;
        private string _statusMessage;
        private bool _isBusy;
        private string _selectedPaymentTerm;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Supplier being added or edited
        /// </summary>
        public Supplier Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value);
        }

        /// <summary>
        /// Flag indicating if this is a new supplier
        /// </summary>
        public bool IsNewSupplier
        {
            get => _isNewSupplier;
            set => SetProperty(ref _isNewSupplier, value);
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
        /// Selected payment term
        /// </summary>
        public string SelectedPaymentTerm
        {
            get => _selectedPaymentTerm;
            set
            {
                if (SetProperty(ref _selectedPaymentTerm, value) && Supplier != null)
                {
                    Supplier.PaymentTerms = value;
                }
            }
        }

        /// <summary>
        /// Collection of available payment terms
        /// </summary>
        public ObservableCollection<string> PaymentTerms { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Command for saving the supplier
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Command for canceling the operation
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the SupplierDialogViewModel class
        /// </summary>
        /// <param name="supplierService">Supplier service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="supplier">Supplier to edit, or null for a new supplier</param>
        public SupplierDialogViewModel(
            ISupplierService supplierService,
            ILogger<SupplierDialogViewModel> logger,
            User currentUser,
            Supplier supplier = null)
            : base(logger)
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Determine if adding or editing
            IsNewSupplier = supplier == null;

            // Set window title
            WindowTitle = IsNewSupplier ? "Add New Supplier" : "Edit Supplier";

            // Initialize supplier
            if (IsNewSupplier)
            {
                Supplier = new Supplier
                {
                    IsActive = true,
                    CreatedById = _currentUser.Id,
                    CurrentBalance = 0
                };
            }
            else
            {
                // Clone the supplier to avoid modifying the original until Save
                Supplier = CloneSupplier(supplier);
                SelectedPaymentTerm = supplier.PaymentTerms;
            }

            // Initialize commands
            SaveCommand = CreateCommand(SaveSupplier, CanSaveSupplier);
            CancelCommand = CreateCommand(Cancel);
        }

        /// <summary>
        /// Loads available payment terms
        /// </summary>
        public void LoadPaymentTerms()
        {
            PaymentTerms.Clear();
            PaymentTerms.Add("Net 15");
            PaymentTerms.Add("Net 30");
            PaymentTerms.Add("Net 45");
            PaymentTerms.Add("Net 60");
            PaymentTerms.Add("COD");
            PaymentTerms.Add("Prepaid");

            // Set default or current payment term
            if (string.IsNullOrEmpty(SelectedPaymentTerm))
            {
                SelectedPaymentTerm = "Net 30";
            }
        }

        /// <summary>
        /// Saves the supplier
        /// </summary>
        private async void SaveSupplier(object parameter)
        {
            if (!ValidateSupplier())
                return;

            try
            {
                IsBusy = true;

                if (IsNewSupplier)
                {
                    StatusMessage = "Creating supplier...";
                    Supplier.CreatedById = _currentUser.Id;
                    Supplier.CreatedDate = DateTime.Now;
                    await _supplierService.CreateSupplierAsync(Supplier);
                }
                else
                {
                    StatusMessage = "Updating supplier...";
                    Supplier.LastUpdatedById = _currentUser.Id;
                    Supplier.LastUpdatedDate = DateTime.Now;
                    await _supplierService.UpdateSupplierAsync(Supplier);
                }

                // Close the dialog with success
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving supplier: {ex.Message}";
                _logger.LogError(ex, "Error saving supplier {SupplierId}", Supplier.Id);
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Validates the supplier data
        /// </summary>
        private bool ValidateSupplier()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Supplier.Name))
            {
                ErrorMessage = "Supplier name is required";
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
        /// Determines if the supplier can be saved
        /// </summary>
        private bool CanSaveSupplier(object parameter)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Supplier?.Name);
        }

        /// <summary>
        /// Creates a clone of a supplier to avoid modifying the original
        /// </summary>
        private Supplier CloneSupplier(Supplier source)
        {
            if (source == null)
                return null;

            return new Supplier
            {
                Id = source.Id,
                Name = source.Name,
                ContactPerson = source.ContactPerson,
                PhoneNumber = source.PhoneNumber,
                Email = source.Email,
                Address = source.Address,
                TaxId = source.TaxId,
                PaymentTerms = source.PaymentTerms,
                CurrentBalance = source.CurrentBalance,
                CreatedById = source.CreatedById,
                CreatedDate = source.CreatedDate,
                IsActive = source.IsActive
            };
        }
    }
}