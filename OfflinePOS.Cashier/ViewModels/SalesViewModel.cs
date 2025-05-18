// OfflinePOS.Cashier/ViewModels/SalesViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Cashier.ViewModels
{
    /// <summary>
    /// ViewModel for the sales screen
    /// </summary>
    public class SalesViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly ITransactionService _transactionService;
        private readonly IDrawerService _drawerService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private DrawerOperation _currentDrawer;

        private ObservableCollection<Product> _products;
        private ObservableCollection<TransactionItemViewModel> _cartItems;
        private TransactionItemViewModel _selectedCartItem;
        private Customer _selectedCustomer;
        private ObservableCollection<Customer> _customers;
        private string _searchText;
        private decimal _subtotal;
        private decimal _discountPercentage;
        private decimal _discountAmount;
        private decimal _taxPercentage;
        private decimal _taxAmount;
        private decimal _total;
        private decimal _amountPaid;
        private decimal _change;
        private string _paymentMethod;
        private bool _isDrawerOpen;
        private ObservableCollection<string> _paymentMethods;

        /// <summary>
        /// Current authenticated user
        /// </summary>
        public User CurrentUser { get; }

        /// <summary>
        /// Available payment methods
        /// </summary>
        public ObservableCollection<string> PaymentMethods
        {
            get => _paymentMethods;
            set => SetProperty(ref _paymentMethods, value);
        }

        /// <summary>
        /// Collection of products matching the search criteria
        /// </summary>
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        /// <summary>
        /// Collection of items in the cart
        /// </summary>
        public ObservableCollection<TransactionItemViewModel> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        /// <summary>
        /// Currently selected cart item
        /// </summary>
        public TransactionItemViewModel SelectedCartItem
        {
            get => _selectedCartItem;
            set => SetProperty(ref _selectedCartItem, value);
        }

        /// <summary>
        /// Selected customer for the transaction
        /// </summary>
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        /// <summary>
        /// Collection of customers
        /// </summary>
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        /// <summary>
        /// Search text for finding products
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    SearchProductsCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Subtotal amount before discounts and taxes
        /// </summary>
        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }

        /// <summary>
        /// Discount percentage
        /// </summary>
        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set
            {
                if (SetProperty(ref _discountPercentage, value))
                {
                    CalculateTotals();
                }
            }
        }

        /// <summary>
        /// Calculated discount amount
        /// </summary>
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => SetProperty(ref _discountAmount, value);
        }

        /// <summary>
        /// Tax percentage
        /// </summary>
        public decimal TaxPercentage
        {
            get => _taxPercentage;
            set
            {
                if (SetProperty(ref _taxPercentage, value))
                {
                    CalculateTotals();
                }
            }
        }

        /// <summary>
        /// Calculated tax amount
        /// </summary>
        public decimal TaxAmount
        {
            get => _taxAmount;
            set => SetProperty(ref _taxAmount, value);
        }

        /// <summary>
        /// Total amount after discounts and taxes
        /// </summary>
        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        /// <summary>
        /// Amount paid by the customer
        /// </summary>
        public decimal AmountPaid
        {
            get => _amountPaid;
            set
            {
                if (SetProperty(ref _amountPaid, value))
                {
                    CalculateChange();
                }
            }
        }

        /// <summary>
        /// Change amount to return to the customer
        /// </summary>
        public decimal Change
        {
            get => _change;
            set => SetProperty(ref _change, value);
        }

        /// <summary>
        /// Payment method (Cash, Card, etc.)
        /// </summary>
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        /// <summary>
        /// Flag indicating if a cash drawer is open
        /// </summary>
        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set => SetProperty(ref _isDrawerOpen, value);
        }

        /// <summary>
        /// Command for searching products
        /// </summary>
        public ICommand SearchProductsCommand { get; }

        /// <summary>
        /// Command for adding a product to the cart
        /// </summary>
        public ICommand AddToCartCommand { get; }

        /// <summary>
        /// Command for removing an item from the cart
        /// </summary>
        public ICommand RemoveFromCartCommand { get; }

        /// <summary>
        /// Command for clearing the cart
        /// </summary>
        public ICommand ClearCartCommand { get; }

        /// <summary>
        /// Command for increasing item quantity
        /// </summary>
        public ICommand IncreaseQuantityCommand { get; }

        /// <summary>
        /// Command for decreasing item quantity
        /// </summary>
        public ICommand DecreaseQuantityCommand { get; }

        /// <summary>
        /// Command for navigating to the drawer view
        /// </summary>
        public ICommand NavigateToDrawerCommand { get; }

        /// <summary>
        /// Command for logging out
        /// </summary>
        public ICommand LogoutCommand { get; }

        /// <summary>
        /// Command for selecting a customer
        /// </summary>
        public ICommand SelectCustomerCommand { get; }

        /// <summary>
        /// Command for processing payment
        /// </summary>
        public ICommand ProcessPaymentCommand { get; }

        /// <summary>
        /// Command for printing a receipt
        /// </summary>
        public ICommand PrintReceiptCommand { get; }

        /// <summary>
        /// Initializes a new instance of the SalesViewModel class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="transactionService">Transaction service</param>
        /// <param name="drawerService">Drawer service</param>
        /// <param name="authService">Authentication service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="navigationService">Navigation service</param>
        public SalesViewModel(
            IProductService productService,
            ITransactionService transactionService,
            IDrawerService drawerService,
            IAuthService authService,
            ILogger<SalesViewModel> logger,
            User currentUser,
            INavigationService navigationService)
            : base(logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _drawerService = drawerService ?? throw new ArgumentNullException(nameof(drawerService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Initialize CurrentUser property
            CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            // Initialize collections
            Products = new ObservableCollection<Product>();
            CartItems = new ObservableCollection<TransactionItemViewModel>();
            Customers = new ObservableCollection<Customer>();

            // Initialize PaymentMethods collection
            PaymentMethods = new ObservableCollection<string>
            {
                "Cash",
                "Credit Card",
                "Debit Card",
                "Credit"
            };

            // Initialize commands
            SearchProductsCommand = new AsyncRelayCommand(_ => SearchProductsAsync());
            AddToCartCommand = new RelayCommand(AddToCart);
            RemoveFromCartCommand = new RelayCommand(RemoveFromCart, CanModifyCart);
            ClearCartCommand = new RelayCommand(_ => ClearCart(), _ => CartItems.Any());
            IncreaseQuantityCommand = new RelayCommand(IncreaseQuantity, CanModifyCart);
            DecreaseQuantityCommand = new RelayCommand(DecreaseQuantity, CanModifyCart);
            SelectCustomerCommand = new RelayCommand(_ => SelectCustomer());
            ProcessPaymentCommand = new AsyncRelayCommand(_ => ProcessPaymentAsync(), CanProcessPayment);
            PrintReceiptCommand = new RelayCommand(PrintReceipt, CanPrintReceipt);

            // Update navigation commands to use the navigation service
            NavigateToDrawerCommand = new RelayCommand(_ => NavigateToDrawer());
            LogoutCommand = new RelayCommand(_ => Logout());

            // Set default values
            PaymentMethod = "Cash";
            TaxPercentage = 11; // Default tax rate (e.g., for Lebanon)
        }

        /// <summary>
        /// Initializes the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            await CheckDrawerStatusAsync();
        }

        /// <summary>
        /// Checks if a cash drawer is open for the current user
        /// </summary>
        private async Task CheckDrawerStatusAsync()
        {
            await ExecuteWithLoadingAsync(
                async () =>
                {
                    _currentDrawer = await _drawerService.GetOpenDrawerForUserAsync(CurrentUser.Id);
                    IsDrawerOpen = _currentDrawer != null;
                    return true;
                },
                "Checking drawer status...",
                "Failed to check drawer status");
        }

        /// <summary>
        /// Searches for products matching the search text
        /// </summary>
        private async Task SearchProductsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            await ExecuteWithLoadingAsync(
                async () =>
                {
                    var products = await _productService.SearchProductsAsync(SearchText);
                    Products.Clear();
                    foreach (var product in products)
                    {
                        Products.Add(product);
                    }
                    return true;
                },
                "Searching products...",
                "Failed to search products");
        }

        /// <summary>
        /// Adds a product to the cart
        /// </summary>
        private void AddToCart(object parameter)
        {
            if (parameter is Product product)
            {
                // Check if product already in cart
                var existingItem = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
                if (existingItem != null)
                {
                    // Increase quantity
                    existingItem.Quantity++;
                    existingItem.CalculateTotals();
                }
                else
                {
                    // Add new item
                    var item = new TransactionItemViewModel
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.ItemSalePrice,
                        UnitType = "Item",
                        Quantity = 1,
                        TaxPercentage = TaxPercentage
                    };
                    item.CalculateTotals();
                    CartItems.Add(item);
                }

                CalculateTotals();
            }
        }

        /// <summary>
        /// Removes the selected item from the cart
        /// </summary>
        private void RemoveFromCart(object parameter)
        {
            if (SelectedCartItem != null)
            {
                CartItems.Remove(SelectedCartItem);
                CalculateTotals();
            }
        }

        /// <summary>
        /// Clears all items from the cart
        /// </summary>
        private void ClearCart()
        {
            CartItems.Clear();
            CalculateTotals();
        }

        /// <summary>
        /// Increases the quantity of the selected cart item
        /// </summary>
        private void IncreaseQuantity(object parameter)
        {
            if (SelectedCartItem != null)
            {
                SelectedCartItem.Quantity++;
                SelectedCartItem.CalculateTotals();
                CalculateTotals();
            }
        }

        /// <summary>
        /// Navigates to the drawer view
        /// </summary>
        private void NavigateToDrawer()
        {
            try
            {
                _logger.LogInformation("Navigating to DrawerView");
                _navigationService.NavigateTo("DrawerView");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to DrawerView");
                ErrorMessage = "Navigation error. Please try again.";
            }
        }

        /// <summary>
        /// Logs out of the application
        /// </summary>
        private void Logout()
        {
            try
            {
                _logger.LogInformation("Logging out user");
                _navigationService.Logout();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                ErrorMessage = "Logout error. Please try again.";
            }
        }

        /// <summary>
        /// Decreases the quantity of the selected cart item
        /// </summary>
        private void DecreaseQuantity(object parameter)
        {
            if (SelectedCartItem != null && SelectedCartItem.Quantity > 1)
            {
                SelectedCartItem.Quantity--;
                SelectedCartItem.CalculateTotals();
                CalculateTotals();
            }
        }

        /// <summary>
        /// Selects a customer for the transaction
        /// </summary>
        private void SelectCustomer()
        {
            // This would typically show a customer selection dialog
            // For now, we'll just clear the selected customer
            SelectedCustomer = null;
        }

        /// <summary>
        /// Processes the payment for the transaction
        /// </summary>
        private async Task ProcessPaymentAsync()
        {
            if (!CartItems.Any())
                return;

            await ExecuteWithLoadingAsync(
                async () =>
                {
                    // Create transaction
                    var transaction = new Transaction
                    {
                        UserId = CurrentUser.Id,
                        CustomerId = SelectedCustomer?.Id,
                        DrawerOperationId = _currentDrawer.Id,
                        InvoiceNumber = await _transactionService.GenerateInvoiceNumberAsync(),
                        Subtotal = Subtotal,
                        DiscountPercentage = DiscountPercentage,
                        DiscountAmount = DiscountAmount,
                        TaxPercentage = TaxPercentage,
                        TaxAmount = TaxAmount,
                        Total = Total,
                        PaidAmount = Math.Min(AmountPaid, Total),
                        RemainingBalance = Math.Max(0, Total - AmountPaid),
                        PaymentMethod = PaymentMethod,
                        TransactionType = "Sale",
                        Status = AmountPaid >= Total ? "Completed" : "Pending",
                        TransactionDate = DateTime.Now,
                        Items = CartItems.Select(i => new TransactionItem
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            UnitType = i.UnitType,
                            UnitPrice = i.UnitPrice,
                            DiscountPercentage = i.DiscountPercentage,
                            DiscountAmount = i.DiscountAmount,
                            TaxPercentage = i.TaxPercentage,
                            TaxAmount = i.TaxAmount,
                            TotalAmount = i.TotalAmount
                        }).ToList()
                    };

                    var savedTransaction = await _transactionService.CreateTransactionAsync(transaction);

                    // If this is a cash transaction, update the drawer's expected balance
                    if (PaymentMethod == "Cash" && _currentDrawer != null)
                    {
                        // Recalculate expected balance after transaction
                        await _drawerService.CalculateExpectedBalanceAsync(_currentDrawer.Id);
                    }

                    // Clear cart after successful transaction
                    ClearCart();
                    AmountPaid = 0;
                    Change = 0;
                    SelectedCustomer = null;

                    // Show success message or print receipt
                    PrintReceipt(savedTransaction);

                    return savedTransaction;
                },
                "Processing transaction...",
                "Failed to process transaction");
        }

        /// <summary>
        /// Determines if a cart item can be modified
        /// </summary>
        private bool CanModifyCart(object parameter)
        {
            return SelectedCartItem != null && !IsLoading;
        }

        /// <summary>
        /// Determines if a payment can be processed
        /// </summary>
        private bool CanProcessPayment(object parameter)
        {
            return CartItems.Any() &&
                   !IsLoading &&
                   IsDrawerOpen &&
                   (AmountPaid >= Total || (SelectedCustomer != null && PaymentMethod != "Cash"));
        }

        /// <summary>
        /// Prints a receipt for a completed transaction
        /// </summary>
        private void PrintReceipt(object parameter)
        {
            // In a real implementation, this would send the transaction to a receipt printer
            // For now, it's just a placeholder
            _logger.LogInformation("Printing receipt...");
        }

        /// <summary>
        /// Determines if a receipt can be printed
        /// </summary>
        private bool CanPrintReceipt(object parameter)
        {
            return parameter is Transaction && !IsLoading;
        }

        /// <summary>
        /// Calculates the transaction totals based on cart items
        /// </summary>
        private void CalculateTotals()
        {
            // Calculate subtotal
            Subtotal = CartItems.Sum(i => i.TotalAmount);

            // Calculate discount
            DiscountAmount = Math.Round(Subtotal * (DiscountPercentage / 100), 2);

            // Calculate tax on discounted amount
            decimal taxableAmount = Subtotal - DiscountAmount;
            TaxAmount = Math.Round(taxableAmount * (TaxPercentage / 100), 2);

            // Calculate total
            Total = Subtotal - DiscountAmount + TaxAmount;

            // Update change amount
            CalculateChange();
        }

        /// <summary>
        /// Calculates the change amount based on amount paid
        /// </summary>
        private void CalculateChange()
        {
            Change = Math.Max(0, AmountPaid - Total);
        }
    }
}