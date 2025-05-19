// OfflinePOS.Admin/ViewModels/CustomerViewModel.cs
using Microsoft.Extensions.Logging;
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
    public class CustomerViewModel : ViewModelCommandBase
    {
        private readonly ICustomerService _customerService;
        private readonly User _currentUser;
        private readonly IServiceProvider _serviceProvider;

        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private string _searchText;
        private bool _isBusy;
        private string _statusMessage;

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
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

        public ICommand SearchCustomersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddCustomerCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand SettleDebtCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        public CustomerViewModel(
            ICustomerService customerService,
            ILogger<CustomerViewModel> logger,
            User currentUser,
            IServiceProvider serviceProvider)
            : base(logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            Customers = new ObservableCollection<Customer>();

            // Initialize commands
            SearchCustomersCommand = CreateCommand(SearchCustomers);
            RefreshCommand = CreateCommand(async _ => await LoadDataAsync());
            AddCustomerCommand = CreateCommand(AddCustomer);
            EditCustomerCommand = CreateCommand(EditCustomer, CanEditCustomer);
            DeleteCustomerCommand = CreateCommand(DeleteCustomer, CanDeleteCustomer);
            SettleDebtCommand = CreateCommand(SettleDebt, CanManageCustomer);
            ViewHistoryCommand = CreateCommand(ViewHistory, CanManageCustomer);
        }

        public async Task LoadDataAsync()
        {
            // Implementation for loading customers
            StatusMessage = "Customer loading will be implemented soon.";
        }

        private void SearchCustomers(object parameter)
        {
            // Implementation for searching customers
            StatusMessage = "Customer search will be implemented soon.";
        }

        private void AddCustomer(object parameter)
        {
            // Implementation for adding a customer
            MessageBox.Show("Add Customer functionality will be implemented soon.");
        }

        private void EditCustomer(object parameter)
        {
            // Implementation for editing a customer
            MessageBox.Show("Edit Customer functionality will be implemented soon.");
        }

        private void DeleteCustomer(object parameter)
        {
            // Implementation for deleting a customer
            MessageBox.Show("Delete Customer functionality will be implemented soon.");
        }

        private void SettleDebt(object parameter)
        {
            // Implementation for settling customer debt
            MessageBox.Show("Settle Customer Debt functionality will be implemented soon.");
        }

        private void ViewHistory(object parameter)
        {
            // Implementation for viewing customer transaction history
            MessageBox.Show("View Customer History functionality will be implemented soon.");
        }

        private bool CanEditCustomer(object parameter)
        {
            return SelectedCustomer != null;
        }

        private bool CanDeleteCustomer(object parameter)
        {
            return SelectedCustomer != null;
        }

        private bool CanManageCustomer(object parameter)
        {
            return SelectedCustomer != null;
        }
    }
}