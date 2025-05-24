// OfflinePOS.Admin/ViewModels/SupplierInvoiceDetailsViewModel.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for displaying detailed information about a supplier invoice
    /// </summary>
    public class SupplierInvoiceDetailsViewModel : ViewModelCommandBase
    {
        private readonly ISupplierInvoiceService _supplierInvoiceService;
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;

        private SupplierInvoice _invoice;
        private Supplier _supplier;
        private ObservableCollection<SupplierPayment> _payments;
        private string _windowTitle;
        private string _statusMessage;
        private bool _isBusy;

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// The invoice being displayed
        /// </summary>
        public SupplierInvoice Invoice
        {
            get => _invoice;
            set => SetProperty(ref _invoice, value);
        }

        /// <summary>
        /// The supplier for this invoice
        /// </summary>
        public Supplier Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value);
        }

        /// <summary>
        /// Payments made against this invoice
        /// </summary>
        public ObservableCollection<SupplierPayment> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }

        /// <summary>
        /// Window title
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
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
        /// Command for closing the window
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Command for making a payment
        /// </summary>
        public ICommand MakePaymentCommand { get; }

        /// <summary>
        /// Command for printing the invoice
        /// </summary>
        public ICommand PrintInvoiceCommand { get; }

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceDetailsViewModel class
        /// </summary>
        public SupplierInvoiceDetailsViewModel(
            ISupplierInvoiceService supplierInvoiceService,
            IProductService productService,
            ILogger<SupplierInvoiceDetailsViewModel> logger,
            User currentUser,
            SupplierInvoice invoice,
            Supplier supplier,
            IServiceProvider serviceProvider = null)
            : base(logger)
        {
            _supplierInvoiceService = supplierInvoiceService ?? throw new ArgumentNullException(nameof(supplierInvoiceService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _serviceProvider = serviceProvider; // This can be null

            // Set invoice and supplier
            Invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            Supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

            // Initialize collections
            Payments = new ObservableCollection<SupplierPayment>();

            // Set window title
            WindowTitle = $"Invoice Details: {Invoice.InvoiceNumber}";

            // Initialize commands
            CloseCommand = CreateCommand(Close);
            MakePaymentCommand = CreateCommand(MakePayment, CanMakePayment);
            PrintInvoiceCommand = CreateCommand(PrintInvoice);
        }

        /// <summary>
        /// Loads data for the invoice
        /// </summary>
        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading invoice details...";

                // Load payments
                var payments = await _supplierInvoiceService.GetPaymentsByInvoiceAsync(Invoice.Id);
                Payments.Clear();
                foreach (var payment in payments)
                {
                    Payments.Add(payment);
                }

                StatusMessage = "Invoice details loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading invoice details: {ex.Message}";
                _logger.LogError(ex, "Error loading details for invoice {InvoiceId}", Invoice.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Closes the window
        /// </summary>
        private void Close(object parameter)
        {
            CloseRequested?.Invoke(this, true);
        }

        /// <summary>
        /// Makes a payment on the invoice
        /// </summary>
        private async void MakePayment(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Preparing payment dialog...";

                if (_serviceProvider == null)
                {
                    StatusMessage = "Service provider is not available";
                    return;
                }

                // Create the payment view model factory via the service provider
                var paymentViewModelFactory = _serviceProvider.GetService<Func<Supplier, SupplierInvoice, SupplierPaymentViewModel>>();
                if (paymentViewModelFactory == null)
                {
                    StatusMessage = "Payment service is not available";
                    return;
                }

                // Create the view model
                var viewModel = paymentViewModelFactory(Supplier, Invoice);

                // Create and show the dialog
                var dialog = new SupplierPaymentDialogView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh if payment was successful
                if (result == true)
                {
                    await LoadDataAsync();
                    StatusMessage = "Payment processed successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing payment: {ex.Message}";
                _logger.LogError(ex, "Error processing payment for invoice {InvoiceId}", Invoice.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Prints the invoice
        /// </summary>
        private void PrintInvoice(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Preparing invoice for printing...";

                // This would integrate with a printing service in a full implementation
                // For now, we'll just display a message

                string printPreviewMessage =
                    $"INVOICE: {Invoice.InvoiceNumber}\n" +
                    $"Date: {Invoice.InvoiceDate:d}\n" +
                    $"Supplier: {Supplier.Name}\n" +
                    $"Amount: {Invoice.TotalAmount:C2}\n" +
                    $"Status: {Invoice.Status}\n\n" +
                    $"This is a preview. Full printing functionality will be implemented in a future update.";

                MessageBox.Show(printPreviewMessage, "Print Preview", MessageBoxButton.OK, MessageBoxImage.Information);

                StatusMessage = "Invoice print preview displayed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error printing invoice: {ex.Message}";
                _logger.LogError(ex, "Error printing invoice {InvoiceId}", Invoice.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Determines if a payment can be made on this invoice
        /// </summary>
        private bool CanMakePayment(object parameter)
        {
            return Invoice != null &&
                   Invoice.Status != "Paid" &&
                   Invoice.Status != "Cancelled" &&
                   !IsBusy;
        }
    }
}