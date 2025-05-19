// OfflinePOS.Admin/ViewModels/TransactionHistoryViewModel.cs
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
    public class TransactionHistoryViewModel : ViewModelCommandBase
    {
        private readonly ITransactionService _transactionService;
        private readonly User _currentUser;

        private ObservableCollection<Transaction> _transactions;
        private Transaction _selectedTransaction;
        private DateTime _dateFrom;
        private DateTime _dateTo;
        private decimal _totalSales;
        private decimal _totalProfit;
        private bool _isBusy;
        private string _statusMessage;

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public Transaction SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
        }

        public DateTime DateFrom
        {
            get => _dateFrom;
            set => SetProperty(ref _dateFrom, value);
        }

        public DateTime DateTo
        {
            get => _dateTo;
            set => SetProperty(ref _dateTo, value);
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        public decimal TotalProfit
        {
            get => _totalProfit;
            set => SetProperty(ref _totalProfit, value);
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

        public ICommand SearchCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand PrintReportCommand { get; }

        public TransactionHistoryViewModel(
            ITransactionService transactionService,
            ILogger<TransactionHistoryViewModel> logger,
            User currentUser)
            : base(logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            Transactions = new ObservableCollection<Transaction>();

            // Initialize date range to past 30 days by default
            DateTo = DateTime.Now;
            DateFrom = DateTo.AddDays(-30);

            // Initialize commands
            SearchCommand = CreateCommand(SearchTransactions);
            ViewDetailsCommand = CreateCommand(ViewTransactionDetails, CanSelectTransaction);
            DeleteInvoiceCommand = CreateCommand(DeleteInvoice, CanSelectTransaction);
            PrintReportCommand = CreateCommand(PrintReport);
        }

        public async Task LoadDataAsync()
        {
            await SearchTransactionsAsync(DateFrom, DateTo);
        }

        private async void SearchTransactions(object parameter)
        {
            await SearchTransactionsAsync(DateFrom, DateTo);
        }

        private async Task SearchTransactionsAsync(DateTime from, DateTime to)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Searching transactions...";

                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(from, to);

                Transactions.Clear();
                decimal totalSales = 0;
                decimal totalProfit = 0;

                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                    totalSales += transaction.Total;

                    // Calculate profit based on transaction items (purchase price vs. sale price)
                    // This is a simplification - in a real system, you'd need to calculate actual profit
                    decimal transactionProfit = 0;
                    foreach (var item in transaction.Items)
                    {
                        decimal costPrice = item.UnitType == "Box" ?
                            (item.Product?.BoxPurchasePrice ?? 0) * item.Quantity :
                            (item.Product?.ItemPurchasePrice ?? 0) * item.Quantity;

                        transactionProfit += item.TotalAmount - costPrice;
                    }

                    totalProfit += transactionProfit;
                }

                TotalSales = totalSales;
                TotalProfit = totalProfit;

                StatusMessage = $"Found {Transactions.Count} transactions";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching transactions: {ex.Message}";
                _logger.LogError(ex, "Error searching transactions from {From} to {To}", from, to);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ViewTransactionDetails(object parameter)
        {
            // Implementation for viewing transaction details
            MessageBox.Show($"Transaction Details: {SelectedTransaction.InvoiceNumber}\nAmount: {SelectedTransaction.Total:C2}",
                "Transaction Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteInvoice(object parameter)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete invoice {SelectedTransaction.InvoiceNumber}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = "Deleting transaction...";

                    // Set status to cancelled instead of actually deleting
                    await _transactionService.UpdateTransactionStatusAsync(SelectedTransaction.Id, "Cancelled");

                    // Refresh data
                    await SearchTransactionsAsync(DateFrom, DateTo);

                    StatusMessage = "Transaction cancelled successfully";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error cancelling transaction: {ex.Message}";
                    _logger.LogError(ex, "Error cancelling transaction {TransactionId}", SelectedTransaction.Id);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void PrintReport(object parameter)
        {
            // Implementation for printing a report of transactions
            MessageBox.Show("Print Report functionality will be implemented soon.");
        }

        private bool CanSelectTransaction(object parameter)
        {
            return SelectedTransaction != null;
        }
    }
}