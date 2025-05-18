// OfflinePOS.Cashier/ViewModels/DrawerViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Cashier.ViewModels
{
    /// <summary>
    /// ViewModel for cash drawer operations
    /// </summary>
    public class DrawerViewModel : ViewModelBase
    {
        private readonly IDrawerService _drawerService;
        private readonly User _currentUser;

        private DrawerOperation _currentDrawer;
        private decimal _drawerBalance;
        private decimal _cashInAmount;
        private decimal _cashOutAmount;
        private string _reason;
        private bool _isDrawerOpen;

        /// <summary>
        /// Current drawer operation
        /// </summary>
        public DrawerOperation CurrentDrawer
        {
            get => _currentDrawer;
            set => SetProperty(ref _currentDrawer, value);
        }

        /// <summary>
        /// Current drawer balance
        /// </summary>
        public decimal DrawerBalance
        {
            get => _drawerBalance;
            set => SetProperty(ref _drawerBalance, value);
        }

        /// <summary>
        /// Amount to add to the drawer
        /// </summary>
        public decimal CashInAmount
        {
            get => _cashInAmount;
            set => SetProperty(ref _cashInAmount, value);
        }

        /// <summary>
        /// Amount to remove from the drawer
        /// </summary>
        public decimal CashOutAmount
        {
            get => _cashOutAmount;
            set => SetProperty(ref _cashOutAmount, value);
        }

        /// <summary>
        /// Reason for cash movement
        /// </summary>
        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        /// <summary>
        /// Flag indicating if a drawer is open
        /// </summary>
        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set => SetProperty(ref _isDrawerOpen, value);
        }

        /// <summary>
        /// Command for opening the drawer
        /// </summary>
        public ICommand OpenDrawerCommand { get; }

        /// <summary>
        /// Command for closing the drawer
        /// </summary>
        public ICommand CloseDrawerCommand { get; }

        /// <summary>
        /// Command for navigating to the sales view
        /// </summary>
        public ICommand NavigateToSalesCommand { get; }
        /// <summary>
        /// Command for adding cash to the drawer
        /// </summary>
        public ICommand CashInCommand { get; }

        /// <summary>
        /// Command for removing cash from the drawer
        /// </summary>
        public ICommand CashOutCommand { get; }

        /// <summary>
        /// Command for viewing shift details
        /// </summary>
        public ICommand ViewShiftDetailsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the DrawerViewModel class
        /// </summary>
        /// <param name="drawerService">Drawer service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        public DrawerViewModel(
            IDrawerService drawerService,
            ILogger<DrawerViewModel> logger,
            User currentUser)
            : base(logger)
        {
            _drawerService = drawerService ?? throw new ArgumentNullException(nameof(drawerService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Initialize commands
            OpenDrawerCommand = new AsyncRelayCommand(_ => OpenDrawerAsync(), CanOpenDrawer);
            CloseDrawerCommand = new AsyncRelayCommand(_ => CloseDrawerAsync(), CanCloseDrawer);
            CashInCommand = new AsyncRelayCommand(_ => CashInAsync(), CanManageCash);
            CashOutCommand = new AsyncRelayCommand(_ => CashOutAsync(), CanManageCash);
            ViewShiftDetailsCommand = new RelayCommand(_ => ViewShiftDetails(), CanViewShiftDetails);
            NavigateToSalesCommand = new RelayCommand(_ => NavigateToSales(), _ => IsDrawerOpen);
        }

        /// <summary>
        /// Initializes the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadCurrentDrawerAsync();
        }


        /// <summary>
        /// Loads the current drawer for the user
        /// </summary>
        private async Task LoadCurrentDrawerAsync()
        {
            await ExecuteWithLoadingAsync(
                async () =>
                {
                    CurrentDrawer = await _drawerService.GetOpenDrawerForUserAsync(_currentUser.Id);
                    IsDrawerOpen = CurrentDrawer != null;

                    if (IsDrawerOpen)
                    {
                        // Load the expected balance for the drawer
                        DrawerBalance = await _drawerService.CalculateExpectedBalanceAsync(CurrentDrawer.Id);
                    }

                    return true;
                },
                "Loading drawer information...",
                "Failed to load drawer information");
        }

        /// <summary>
        /// Opens a new cash drawer
        /// </summary>
        private async Task OpenDrawerAsync()
        {
            if (CashInAmount <= 0)
            {
                ErrorMessage = "Starting balance must be greater than zero";
                return;
            }

            await ExecuteWithLoadingAsync(
                async () =>
                {
                    CurrentDrawer = await _drawerService.OpenDrawerAsync(_currentUser.Id, CashInAmount);
                    DrawerBalance = CashInAmount;
                    IsDrawerOpen = true;
                    CashInAmount = 0;
                    return CurrentDrawer;
                },
                "Opening drawer...",
                "Failed to open drawer",
                _ => _logger.LogInformation("Drawer opened with ID: {DrawerId}", CurrentDrawer.Id));
        }
        /// <summary>
        /// Navigates to the sales view
        /// </summary>
        private void NavigateToSales()
        {
            // This will be handled by the application shell
            _logger.LogInformation("Requested navigation to sales view");
        }

        /// <summary>
        /// Closes the current cash drawer
        /// </summary>
        private async Task CloseDrawerAsync()
        {
            await ExecuteWithLoadingAsync(
                async () =>
                {
                    var closedDrawer = await _drawerService.CloseDrawerAsync(
                        CurrentDrawer.Id,
                        CashOutAmount,
                        Reason ?? "End of shift");

                    IsDrawerOpen = false;
                    CurrentDrawer = null;
                    DrawerBalance = 0;
                    CashOutAmount = 0;
                    Reason = string.Empty;

                    return closedDrawer;
                },
                "Closing drawer...",
                "Failed to close drawer",
                closedDrawer => _logger.LogInformation("Drawer closed with ID: {DrawerId}", closedDrawer.Id));
        }

        /// <summary>
        /// Adds cash to the drawer
        /// </summary>
        private async Task CashInAsync()
        {
            if (CashInAmount <= 0)
            {
                ErrorMessage = "Cash in amount must be greater than zero";
                return;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                ErrorMessage = "Please provide a reason for adding cash";
                return;
            }

            await ExecuteWithLoadingAsync(
                async () =>
                {
                    var transaction = await _drawerService.RecordCashInAsync(
                        CurrentDrawer.Id,
                        CashInAmount,
                        Reason,
                        $"Manual cash in - {DateTime.Now:G}",
                        _currentUser.Id);

                    // Update drawer balance
                    DrawerBalance += CashInAmount;
                    CashInAmount = 0;
                    Reason = string.Empty;

                    return transaction;
                },
                "Adding cash...",
                "Failed to add cash",
                _ => _logger.LogInformation("Cash added to drawer: {Amount}", CashInAmount));
        }

        /// <summary>
        /// Removes cash from the drawer
        /// </summary>
        private async Task CashOutAsync()
        {
            if (CashOutAmount <= 0)
            {
                ErrorMessage = "Cash out amount must be greater than zero";
                return;
            }

            if (CashOutAmount > DrawerBalance)
            {
                ErrorMessage = "Not enough cash in drawer";
                return;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                ErrorMessage = "Please provide a reason for removing cash";
                return;
            }

            await ExecuteWithLoadingAsync(
                async () =>
                {
                    var transaction = await _drawerService.RecordCashOutAsync(
                        CurrentDrawer.Id,
                        CashOutAmount,
                        Reason,
                        $"Manual cash out - {DateTime.Now:G}",
                        _currentUser.Id);

                    // Update drawer balance
                    DrawerBalance -= CashOutAmount;
                    CashOutAmount = 0;
                    Reason = string.Empty;

                    return transaction;
                },
                "Removing cash...",
                "Failed to remove cash",
                _ => _logger.LogInformation("Cash removed from drawer: {Amount}", CashOutAmount));
        }

        /// <summary>
        /// Views detailed information about the current shift
        /// </summary>
        private void ViewShiftDetails()
        {
            // In a real implementation, this would navigate to a shift details view
            _logger.LogInformation("Viewing shift details for drawer ID: {DrawerId}", CurrentDrawer.Id);
        }

        /// <summary>
        /// Determines if a drawer can be opened
        /// </summary>
        private bool CanOpenDrawer(object parameter)
        {
            return !IsDrawerOpen && !IsLoading;
        }

        /// <summary>
        /// Determines if a drawer can be closed
        /// </summary>
        private bool CanCloseDrawer(object parameter)
        {
            return IsDrawerOpen && !IsLoading;
        }

        /// <summary>
        /// Determines if cash can be managed
        /// </summary>
        private bool CanManageCash(object parameter)
        {
            return IsDrawerOpen && !IsLoading;
        }

        /// <summary>
        /// Determines if shift details can be viewed
        /// </summary>
        private bool CanViewShiftDetails(object parameter)
        {
            return IsDrawerOpen && !IsLoading;
        }
    }
}