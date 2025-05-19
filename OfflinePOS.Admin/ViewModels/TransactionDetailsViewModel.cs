// OfflinePOS.Admin/ViewModels/TransactionDetailsViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    public class TransactionDetailsViewModel : ViewModelCommandBase
    {
        private readonly ITransactionService _transactionService;
        private Transaction _transaction;
        private ObservableCollection<TransactionItem> _transactionItems;

        /// <summary>
        /// Transaction being viewed
        /// </summary>
        public Transaction Transaction
        {
            get => _transaction;
            set => SetProperty(ref _transaction, value);
        }

        /// <summary>
        /// Collection of transaction items
        /// </summary>
        public ObservableCollection<TransactionItem> TransactionItems
        {
            get => _transactionItems;
            set => SetProperty(ref _transactionItems, value);
        }

        /// <summary>
        /// Command for closing the dialog
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Command for printing the receipt
        /// </summary>
        public ICommand PrintReceiptCommand { get; }

        /// <summary>
        /// Initializes a new instance of the TransactionDetailsViewModel class
        /// </summary>
        /// <param name="transactionService">Transaction service</param>
        /// <param name="logger">Logger</param>
        /// <param name="transaction">Transaction to view</param>
        public TransactionDetailsViewModel(
            ITransactionService transactionService,
            ILogger<TransactionDetailsViewModel> logger,
            Transaction transaction)
            : base(logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

            // Initialize transaction items
            TransactionItems = new ObservableCollection<TransactionItem>();
            if (transaction.Items != null)
            {
                foreach (var item in transaction.Items)
                {
                    TransactionItems.Add(item);
                }
            }

            // Initialize commands
            CloseCommand = CreateCommand(Close);
            PrintReceiptCommand = CreateCommand(PrintReceipt);
        }

        /// <summary>
        /// Closes the dialog
        /// </summary>
        private void Close(object parameter)
        {
            // Since we're just closing the window, we don't need to do anything here
            // The dialog host will handle the closing
        }

        /// <summary>
        /// Prints the transaction receipt
        /// </summary>
        private void PrintReceipt(object parameter)
        {
            // This would connect to a receipt printer in a real implementation
            _logger.LogInformation($"Printing receipt for transaction {Transaction.InvoiceNumber}");

            // For now, we'll just show a message indicating that printing is not implemented
            System.Windows.MessageBox.Show(
                "Receipt printing is not implemented in this version.",
                "Print Receipt",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
}