// OfflinePOS.Admin/ViewModels/SupplierViewModel.cs
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

        private void AddSupplier(object parameter)
        {
            // Implementation for adding a supplier - will need a SupplierDialogViewModel and View
            MessageBox.Show("Add Supplier functionality will be implemented soon.");
        }

        private void EditSupplier(object parameter)
        {
            // Implementation for editing a supplier
            MessageBox.Show("Edit Supplier functionality will be implemented soon.");
        }

        private void DeleteSupplier(object parameter)
        {
            // Implementation for deleting a supplier
            if (SelectedSupplier == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete supplier '{SelectedSupplier.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Delete logic here
                MessageBox.Show("Delete Supplier functionality will be implemented soon.");
            }
        }

        private void ViewInvoices(object parameter)
        {
            // Implementation for viewing supplier invoices
            MessageBox.Show("View Supplier Invoices functionality will be implemented soon.");
        }

        private void CreateInvoice(object parameter)
        {
            // Implementation for creating a supplier invoice
            MessageBox.Show("Create Supplier Invoice functionality will be implemented soon.");
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