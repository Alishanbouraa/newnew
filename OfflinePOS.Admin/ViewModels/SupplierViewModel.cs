// OfflinePOS.Admin/ViewModels/SupplierViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public class SupplierViewModel : ViewModelCommandBase
    {
        private readonly ISupplierService _supplierService;
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;

        private ObservableCollection<Supplier> _suppliers;
        private Supplier _selectedSupplier;
        private string _searchText;
        private bool _isBusy;
        private string _statusMessage;

        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SearchSuppliersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddSupplierCommand { get; }
        public ICommand EditSupplierCommand { get; }
        public ICommand DeleteSupplierCommand { get; }
        public ICommand ViewInvoicesCommand { get; }
        public ICommand CreateInvoiceCommand { get; }

        public SupplierViewModel(
            ISupplierService supplierService,
            ILogger<SupplierViewModel> logger,
            User currentUser,
            IServiceProvider serviceProvider)
            : base(logger)
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            Suppliers = new ObservableCollection<Supplier>();

            // Initialize commands
            SearchSuppliersCommand = CreateCommand(SearchSuppliers);
            RefreshCommand = CreateCommand(async _ => await LoadDataAsync());
            AddSupplierCommand = CreateCommand(AddSupplier);
            EditSupplierCommand = CreateCommand(EditSupplier, CanEditSupplier);
            DeleteSupplierCommand = CreateCommand(DeleteSupplier, CanDeleteSupplier);
            ViewInvoicesCommand = CreateCommand(ViewInvoices, CanSelectSupplier);
            CreateInvoiceCommand = CreateCommand(CreateInvoice, CanSelectSupplier);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading suppliers...";

                var suppliers = await _supplierService.GetAllSuppliersAsync();

                Suppliers.Clear();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }

                StatusMessage = $"Loaded {Suppliers.Count} suppliers";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading suppliers: {ex.Message}";
                _logger.LogError(ex, "Error loading suppliers");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void SearchSuppliers(object parameter)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadDataAsync();
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Searching suppliers...";

                var suppliers = await _supplierService.SearchSuppliersAsync(SearchText);

                Suppliers.Clear();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }

                StatusMessage = $"Found {Suppliers.Count} suppliers";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching suppliers: {ex.Message}";
                _logger.LogError(ex, "Error searching suppliers with term: {SearchTerm}", SearchText);
            }
            finally
            {
                IsBusy = false;
            }
        }
        // Update to OfflinePOS.Admin/ViewModels/SupplierViewModel.cs
        private async void AddSupplier(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Preparing to add supplier...";

                // Create a new supplier object
                var supplier = new Supplier
                {
                    IsActive = true,
                    CreatedById = _currentUser.Id,
                    CreatedDate = DateTime.Now,
                    CurrentBalance = 0
                };

                // Create dialog view model
                var viewModel = new SupplierDialogViewModel(
                    _supplierService,
                    _serviceProvider.GetService<ILogger<SupplierDialogViewModel>>(),
                    _currentUser,
                    null);  // Null indicates a new supplier

                // Create and show the dialog
                var dialog = new SupplierDialogView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh suppliers if dialog was successful
                if (result == true)
                {
                    await LoadDataAsync();
                    StatusMessage = "Supplier added successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding supplier: {ex.Message}";
                _logger.LogError(ex, "Error adding a new supplier");
            }
            finally
            {
                IsBusy = false;
            }
        }

           private async void EditSupplier(object parameter)
        {
            var supplier = parameter as Supplier ?? SelectedSupplier;
            if (supplier == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading supplier details...";

                // Get the complete supplier with all properties
                var completeSupplier = await _supplierService.GetSupplierByIdAsync(supplier.Id);
                if (completeSupplier == null)
                {
                    StatusMessage = "Supplier not found";
                    return;
                }

                // Create dialog view model
                var viewModel = new SupplierDialogViewModel(
                    _supplierService,
                    _serviceProvider.GetService<ILogger<SupplierDialogViewModel>>(),
                    _currentUser,
                    completeSupplier);

                // Create and show the dialog
                var dialog = new SupplierDialogView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh suppliers if dialog was successful
                if (result == true)
                {
                    await LoadDataAsync();
                    StatusMessage = "Supplier updated successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing supplier: {ex.Message}";
                _logger.LogError(ex, "Error editing supplier {SupplierId}", supplier.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // OfflinePOS.Admin/ViewModels/SupplierViewModel.cs - Update DeleteSupplier method
        private async void DeleteSupplier(object parameter)
        {
            var supplier = parameter as Supplier ?? SelectedSupplier;
            if (supplier == null)
                return;

            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete supplier '{supplier.Name}'?\n\nThis cannot be undone and may affect related products and invoices.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    IsBusy = true;
                    StatusMessage = "Deleting supplier...";

                    var success = await _supplierService.DeleteSupplierAsync(supplier.Id);

                    if (success)
                    {
                        await LoadDataAsync();
                        StatusMessage = $"Supplier '{supplier.Name}' deleted successfully";

                        // Clear selection
                        SelectedSupplier = null;
                    }
                    else
                    {
                        StatusMessage = "Failed to delete supplier: Supplier not found";
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle business rule violations (e.g., supplier in use)
                StatusMessage = $"Cannot delete supplier: {ex.Message}";
                MessageBox.Show(
                    ex.Message,
                    "Cannot Delete Supplier",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting supplier: {ex.Message}";
                _logger.LogError(ex, "Error deleting supplier {SupplierId}", supplier.Id);

                MessageBox.Show(
                    $"An error occurred while deleting the supplier: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // File: OfflinePOS.Admin/ViewModels/SupplierViewModel.cs

        private void ViewInvoices(object parameter)
        {
            var supplier = parameter as Supplier ?? SelectedSupplier;
            if (supplier == null)
                return;

            try
            {
                // Get services from service provider
                var supplierInvoiceService = _serviceProvider.GetService<ISupplierInvoiceService>();
                if (supplierInvoiceService == null)
                {
                    StatusMessage = "Supplier invoice service is not available";
                    return;
                }

                // Create the view model
                var viewModel = new SupplierInvoiceListViewModel(
                    supplierInvoiceService,
                    _supplierService,
                    _serviceProvider.GetService<IProductService>(), // Add IProductService parameter
                    _serviceProvider.GetService<ILogger<SupplierInvoiceListViewModel>>(),
                    _currentUser,
                    _serviceProvider,
                    supplier);  // Pass the supplier parameter

                // Create and show the dialog
                var dialog = new SupplierInvoiceListView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing invoices: {ex.Message}";
                _logger.LogError(ex, "Error viewing invoices for supplier {SupplierId}", supplier.Id);
            }
        }

        private async void CreateInvoice(object parameter)
        {
            var supplier = parameter as Supplier ?? SelectedSupplier;
            if (supplier == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Preparing to create invoice...";

                // Get required services
                var supplierInvoiceService = _serviceProvider.GetService<ISupplierInvoiceService>();
                var productService = _serviceProvider.GetService<IProductService>();

                if (supplierInvoiceService == null || productService == null)
                {
                    StatusMessage = "Required services are not available";
                    return;
                }

                // Create the view model
                var viewModel = new SupplierInvoiceDialogViewModel(
                    supplierInvoiceService,
                    _supplierService,  // This should be ISupplierService
                    _serviceProvider.GetService<ILogger<SupplierInvoiceDialogViewModel>>(),
                    _currentUser,
                    supplier);

                // Create and show the dialog
                var dialog = new SupplierInvoiceDialogView(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                var result = dialog.ShowDialog();

                // Refresh if dialog was successful
                if (result == true)
                {
                    await LoadDataAsync();
                    StatusMessage = "Supplier invoice created successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating invoice: {ex.Message}";
                _logger.LogError(ex, "Error creating invoice for supplier {SupplierId}", supplier.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private bool CanEditSupplier(object parameter)
        {
            return SelectedSupplier != null;
        }

        private bool CanDeleteSupplier(object parameter)
        {
            return SelectedSupplier != null;
        }

        private bool CanSelectSupplier(object parameter)
        {
            return SelectedSupplier != null;
        }
    }
}