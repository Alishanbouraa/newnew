﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfflinePOS.Admin.Views;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for displaying supplier invoices
    /// </summary>
    public class SupplierInvoiceListViewModel : ViewModelCommandBase
    {
        private readonly ISupplierInvoiceService _supplierInvoiceService;
        private readonly ISupplierService _supplierService;
        private readonly IProductService _productService;
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;

        private Supplier _supplier;
        private ObservableCollection<SupplierInvoice> _invoices;
        private SupplierInvoice _selectedInvoice;
        private ObservableCollection<Product> _invoiceProducts;
        private decimal _totalProductsValue;
        private DateTime _dateFrom;
        private DateTime _dateTo;
        private bool _isBusy;
        private string _statusMessage;
        private string _windowTitle;
        private decimal _totalUnpaid;

        /// <summary>
        /// The supplier whose invoices are being displayed
        /// </summary>
        public Supplier Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value);
        }

        /// <summary>
        /// Collection of supplier invoices
        /// </summary>
        public ObservableCollection<SupplierInvoice> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }

        /// <summary>
        /// Currently selected invoice
        /// </summary>
        public SupplierInvoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                if (SetProperty(ref _selectedInvoice, value))
                {
                    // Clear products when selection changes
                    InvoiceProducts.Clear();
                    TotalProductsValue = 0;

                    // Load products for the selected invoice
                    if (value != null)
                    {
                        LoadInvoiceProducts(value.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Products associated with the selected invoice
        /// </summary>
        public ObservableCollection<Product> InvoiceProducts
        {
            get => _invoiceProducts;
            set => SetProperty(ref _invoiceProducts, value);
        }

        /// <summary>
        /// Total value of products in the invoice
        /// </summary>
        public decimal TotalProductsValue
        {
            get => _totalProductsValue;
            set => SetProperty(ref _totalProductsValue, value);
        }

        /// <summary>
        /// Start date for filtering
        /// </summary>
        public DateTime DateFrom
        {
            get => _dateFrom;
            set => SetProperty(ref _dateFrom, value);
        }

        /// <summary>
        /// End date for filtering
        /// </summary>
        public DateTime DateTo
        {
            get => _dateTo;
            set => SetProperty(ref _dateTo, value);
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
        /// Status message to display
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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
        /// Total unpaid amount
        /// </summary>
        public decimal TotalUnpaid
        {
            get => _totalUnpaid;
            set => SetProperty(ref _totalUnpaid, value);
        }

        /// <summary>
        /// Event raised when the window should be closed
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        /// <summary>
        /// Command for searching invoices
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Command for viewing invoice details
        /// </summary>
        public ICommand ViewDetailsCommand { get; }

        /// <summary>
        /// Command for adding a new invoice
        /// </summary>
        public ICommand AddInvoiceCommand { get; }

        /// <summary>
        /// Command for making a payment
        /// </summary>
        public ICommand MakePaymentCommand { get; }

        /// <summary>
        /// Command for cancelling an invoice
        /// </summary>
        public ICommand CancelInvoiceCommand { get; }

        /// <summary>
        /// Command for refreshing the list
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command for closing the window
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Initializes a new instance of the SupplierInvoiceListViewModel class
        /// </summary>
        public SupplierInvoiceListViewModel(
            ISupplierInvoiceService supplierInvoiceService,
            ISupplierService supplierService,
            IProductService productService,
            ILogger<SupplierInvoiceListViewModel> logger,
            User currentUser,
            IServiceProvider serviceProvider,
            Supplier supplier)
            : base(logger)
        {
            _supplierInvoiceService = supplierInvoiceService ?? throw new ArgumentNullException(nameof(supplierInvoiceService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

            // Initialize collections
            Invoices = new ObservableCollection<SupplierInvoice>();
            InvoiceProducts = new ObservableCollection<Product>();

            // Set default date range to last 3 months
            DateTo = DateTime.Today;
            DateFrom = DateTo.AddMonths(-3);

            // Set window title
            WindowTitle = $"Invoices for {Supplier.Name}";

            // Initialize commands
            SearchCommand = CreateCommand(SearchInvoices);
            ViewDetailsCommand = CreateCommand(ViewInvoiceDetails, CanSelectInvoice);
            AddInvoiceCommand = CreateCommand(AddInvoice);
            MakePaymentCommand = CreateCommand(MakePayment, CanMakePayment);
            CancelInvoiceCommand = CreateCommand(CancelInvoice, CanCancelInvoice);
            RefreshCommand = CreateCommand(async _ => await LoadInvoicesAsync());
            CloseCommand = CreateCommand(Close);
        }

        /// <summary>
        /// Loads invoices for the supplier
        /// </summary>
        public async Task LoadInvoicesAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading invoices...";

                // Get invoices for the supplier within the date range
                var invoices = await _supplierInvoiceService.GetInvoicesBySupplierAsync(Supplier.Id);

                // Filter by date range
                var filteredInvoices = invoices.Where(i =>
                    i.InvoiceDate >= DateFrom && i.InvoiceDate <= DateTo.AddDays(1).AddTicks(-1));

                Invoices.Clear();
                foreach (var invoice in filteredInvoices)
                {
                    Invoices.Add(invoice);
                }

                // Calculate total unpaid
                TotalUnpaid = Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .Sum(i => i.RemainingBalance);

                StatusMessage = $"Loaded {Invoices.Count} invoices";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading invoices: {ex.Message}";
                _logger.LogError(ex, "Error loading invoices for supplier {SupplierId}", Supplier.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads products associated with an invoice
        /// </summary>
        /// <param name="invoiceId">Invoice ID</param>
        private async void LoadInvoiceProducts(int invoiceId)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading products for invoice...";

                // Get products linked to this invoice
                var products = await _productService.GetProductsBySupplierInvoiceAsync(invoiceId);

                InvoiceProducts.Clear();
                TotalProductsValue = 0;

                foreach (var product in products)
                {
                    InvoiceProducts.Add(product);

                    // Sum up product purchase prices (box prices only for simplicity)
                    TotalProductsValue += product.BoxPurchasePrice;
                }

                StatusMessage = $"Loaded {InvoiceProducts.Count} products for invoice";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading products: {ex.Message}";
                _logger.LogError(ex, "Error loading products for invoice {InvoiceId}", invoiceId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Searches for invoices based on date range
        /// </summary>
        private async void SearchInvoices(object parameter)
        {
            await LoadInvoicesAsync();
        }

        /// <summary>
        /// Views details of the selected invoice
        /// </summary>
        private void ViewInvoiceDetails(object parameter)
        {
            var invoice = parameter as SupplierInvoice ?? SelectedInvoice;
            if (invoice == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading invoice details...";

                // Create the view model
                var viewModel = new SupplierInvoiceDetailsViewModel(
                    _supplierInvoiceService,
                    _productService,
                    _serviceProvider.GetService<ILogger<SupplierInvoiceDetailsViewModel>>(),
                    _currentUser,
                    invoice,
                    _supplier,
                    _serviceProvider);

                // Create and show the details view
                var detailsView = new SupplierInvoiceDetailsView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                detailsView.ShowDialog();

                StatusMessage = "Invoice details viewed successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing invoice details: {ex.Message}";
                _logger.LogError(ex, "Error viewing details for invoice {InvoiceId}", invoice.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adds a new invoice for the supplier
        /// </summary>
        private async void AddInvoice(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Preparing to add invoice...";

                // Get required services
                var supplierInvoiceService = _serviceProvider.GetService<ISupplierInvoiceService>();
                if (supplierInvoiceService == null)
                {
                    StatusMessage = "Supplier invoice service is not available";
                    return;
                }

                // Create the view model
                var viewModel = new SupplierInvoiceDialogViewModel(
                    supplierInvoiceService,
                    _supplierService,
                    _serviceProvider.GetService<ILogger<SupplierInvoiceDialogViewModel>>(),
                    _currentUser,
                    Supplier);

                // Create and show the dialog
                var dialog = new SupplierInvoiceDialogView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh if dialog was successful
                if (result == true)
                {
                    await LoadInvoicesAsync();
                    StatusMessage = "Invoice added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding invoice: {ex.Message}";
                _logger.LogError(ex, "Error adding invoice for supplier {SupplierId}", Supplier.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Makes a payment on the selected invoice
        /// </summary>
        private async void MakePayment(object parameter)
        {
            var invoice = parameter as SupplierInvoice ?? SelectedInvoice;
            if (invoice == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Preparing payment dialog...";

                // Create the payment view model
                var viewModel = new SupplierPaymentViewModel(
                    _supplierInvoiceService,
                    _serviceProvider.GetService<ILogger<SupplierPaymentViewModel>>(),
                    _currentUser,
                    _supplier,
                    invoice);

                // Create and show the dialog
                var dialog = new SupplierPaymentDialogView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh invoices if payment was successful
                if (result == true)
                {
                    await LoadInvoicesAsync();
                    StatusMessage = "Payment processed successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing payment: {ex.Message}";
                _logger.LogError(ex, "Error processing payment for invoice {InvoiceId}", invoice.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Cancels the selected invoice
        /// </summary>
        private async void CancelInvoice(object parameter)
        {
            if (SelectedInvoice == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to cancel invoice {SelectedInvoice.InvoiceNumber}?\n\nThis will update the supplier's balance.",
                "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = "Cancelling invoice...";

                    // Prompt for reason
                    string reason = "Cancelled by user"; // In a real app, you'd show a dialog to get this

                    // Cancel the invoice
                    await _supplierInvoiceService.CancelInvoiceAsync(SelectedInvoice.Id, reason, _currentUser.Id);

                    // Refresh the list
                    await LoadInvoicesAsync();

                    StatusMessage = $"Invoice {SelectedInvoice.InvoiceNumber} cancelled successfully";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error cancelling invoice: {ex.Message}";
                    _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", SelectedInvoice.Id);
                }
                finally
                {
                    IsBusy = false;
                }
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
        /// Determines if an invoice can be selected
        /// </summary>
        private bool CanSelectInvoice(object parameter)
        {
            return SelectedInvoice != null && !IsBusy;
        }

        /// <summary>
        /// Determines if a payment can be made
        /// </summary>
        private bool CanMakePayment(object parameter)
        {
            return SelectedInvoice != null &&
                   SelectedInvoice.Status != "Paid" &&
                   SelectedInvoice.Status != "Cancelled" &&
                   !IsBusy;
        }

        /// <summary>
        /// Determines if an invoice can be cancelled
        /// </summary>
        private bool CanCancelInvoice(object parameter)
        {
            return SelectedInvoice != null &&
                   SelectedInvoice.Status != "Cancelled" &&
                   !IsBusy;
        }
    }
}