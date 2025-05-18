// OfflinePOS.Admin/ViewModels/StockManagementViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for managing product stock
    /// </summary>
    public class StockManagementViewModel : InventoryViewModelBase
    {
        private Stock _selectedStock;
        private int _adjustBoxQuantity;
        private int _adjustItemQuantity;
        private string _adjustmentType;
        private string _adjustmentReason;
        private string _locationCode;
        private int _minimumBoxLevel;
        private int _minimumItemLevel;
        private int _reorderBoxQuantity;
        private ObservableCollection<StockAdjustment> _stockAdjustments;

        /// <summary>
        /// Currently selected stock
        /// </summary>
        public Stock SelectedStock
        {
            get => _selectedStock;
            set => SetProperty(ref _selectedStock, value);
        }

        /// <summary>
        /// Box quantity for adjustment
        /// </summary>
        public int AdjustBoxQuantity
        {
            get => _adjustBoxQuantity;
            set => SetProperty(ref _adjustBoxQuantity, value);
        }

        /// <summary>
        /// Item quantity for adjustment
        /// </summary>
        public int AdjustItemQuantity
        {
            get => _adjustItemQuantity;
            set => SetProperty(ref _adjustItemQuantity, value);
        }

        /// <summary>
        /// Type of adjustment (Addition/Reduction/Inventory)
        /// </summary>
        public string AdjustmentType
        {
            get => _adjustmentType;
            set => SetProperty(ref _adjustmentType, value);
        }

        /// <summary>
        /// Reason for adjustment
        /// </summary>
        public string AdjustmentReason
        {
            get => _adjustmentReason;
            set => SetProperty(ref _adjustmentReason, value);
        }

        /// <summary>
        /// Location code for the product
        /// </summary>
        public string LocationCode
        {
            get => _locationCode;
            set => SetProperty(ref _locationCode, value);
        }

        /// <summary>
        /// Minimum box level for reordering
        /// </summary>
        public int MinimumBoxLevel
        {
            get => _minimumBoxLevel;
            set => SetProperty(ref _minimumBoxLevel, value);
        }

        /// <summary>
        /// Minimum item level for reordering
        /// </summary>
        public int MinimumItemLevel
        {
            get => _minimumItemLevel;
            set => SetProperty(ref _minimumItemLevel, value);
        }

        /// <summary>
        /// Quantity of boxes to reorder
        /// </summary>
        public int ReorderBoxQuantity
        {
            get => _reorderBoxQuantity;
            set => SetProperty(ref _reorderBoxQuantity, value);
        }

        /// <summary>
        /// Collection of stock adjustments for the selected product
        /// </summary>
        public ObservableCollection<StockAdjustment> StockAdjustments
        {
            get => _stockAdjustments;
            set => SetProperty(ref _stockAdjustments, value);
        }

        /// <summary>
        /// List of available adjustment types
        /// </summary>
        public ObservableCollection<string> AdjustmentTypes { get; } = new ObservableCollection<string>
        {
            "Addition",
            "Reduction",
            "Inventory"
        };

        /// <summary>
        /// Command for saving stock settings
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Command for adjusting stock
        /// </summary>
        public ICommand AdjustStockCommand { get; }

        /// <summary>
        /// Command for performing inventory count
        /// </summary>
        public ICommand PerformInventoryCommand { get; }

        /// <summary>
        /// Command for converting boxes to items
        /// </summary>
        public ICommand ConvertBoxesToItemsCommand { get; }

        /// <summary>
        /// Command for updating location code
        /// </summary>
        public ICommand UpdateLocationCommand { get; }

        /// <summary>
        /// Command for loading low stock products
        /// </summary>
        public ICommand LoadLowStockCommand { get; }

        /// <summary>
        /// Command for printing stock report
        /// </summary>
        public ICommand PrintStockReportCommand { get; }

        /// <summary>
        /// Initializes a new instance of the StockManagementViewModel class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="stockService">Stock service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        public StockManagementViewModel(
            IProductService productService,
            IStockService stockService,
            ILogger<StockManagementViewModel> logger,
            User currentUser)
            : base(productService, stockService, logger, currentUser)
        {
            // Initialize collections
            StockAdjustments = new ObservableCollection<StockAdjustment>();

            // Initialize commands
            SaveSettingsCommand = CreateCommand(SaveStockSettings, CanManageStock);
            AdjustStockCommand = CreateCommand(AdjustStock, CanAdjustStock);
            PerformInventoryCommand = CreateCommand(PerformInventory, CanManageStock);
            ConvertBoxesToItemsCommand = CreateCommand(ConvertBoxesToItems, CanConvertBoxes);
            UpdateLocationCommand = CreateCommand(UpdateLocation, CanManageStock);
            LoadLowStockCommand = CreateCommand(LoadLowStockProducts);
            PrintStockReportCommand = CreateCommand(PrintStockReport);

            // Set default adjustment type
            AdjustmentType = "Addition";
        }

        /// <summary>
        /// Loads products from the database
        /// </summary>
        protected override async Task LoadProductsAsync()
        {
            await base.LoadProductsAsync();

            // Clear selected product and stock
            SelectedProduct = null;
            SelectedStock = null;
        }

        /// <summary>
        /// Loads the stock for the selected product
        /// </summary>
        /// <param name="product">Product to load stock for</param>
        private async Task LoadProductStockAsync(Product product)
        {
            if (product == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading stock information...";

                SelectedStock = await _stockService.GetStockByProductIdAsync(product.Id);

                // Update UI fields
                AdjustBoxQuantity = 0;
                AdjustItemQuantity = 0;
                AdjustmentReason = string.Empty;
                LocationCode = SelectedStock.LocationCode;
                MinimumBoxLevel = SelectedStock.MinimumBoxLevel;
                MinimumItemLevel = SelectedStock.MinimumItemLevel;
                ReorderBoxQuantity = SelectedStock.ReorderBoxQuantity;

                // Load adjustment history
                await LoadStockAdjustmentHistoryAsync(product.Id);

                StatusMessage = "Stock information loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading stock: {ex.Message}";
                _logger.LogError(ex, "Error loading stock for product {ProductId}", product.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads stock adjustment history for a product
        /// </summary>
        /// <param name="productId">Product ID</param>
        private async Task LoadStockAdjustmentHistoryAsync(int productId)
        {
            try
            {
                // Get the last 30 days of adjustment history
                var endDate = DateTime.Now;
                var startDate = endDate.AddDays(-30);

                var adjustments = await _stockService.GetStockAdjustmentHistoryAsync(
                    productId, startDate, endDate);

                StockAdjustments.Clear();
                foreach (var adjustment in adjustments)
                {
                    StockAdjustments.Add(adjustment);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock adjustment history for product {ProductId}", productId);
            }
        }

        /// <summary>
        /// Saves stock settings for the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void SaveStockSettings(object parameter)
        {
            if (SelectedProduct == null || SelectedStock == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Saving stock settings...";

                await _stockService.UpdateMinimumStockLevelsAsync(
                    SelectedProduct.Id,
                    MinimumBoxLevel,
                    MinimumItemLevel,
                    ReorderBoxQuantity,
                    _currentUser.Id);

                StatusMessage = "Stock settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving stock settings: {ex.Message}";
                _logger.LogError(ex, "Error saving stock settings for product {ProductId}", SelectedProduct.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adjusts stock for the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void AdjustStock(object parameter)
        {
            if (SelectedProduct == null || SelectedStock == null)
                return;

            // Validate input
            if (AdjustBoxQuantity <= 0 && AdjustItemQuantity <= 0)
            {
                StatusMessage = "Please enter a valid quantity to adjust";
                return;
            }

            if (string.IsNullOrWhiteSpace(AdjustmentReason))
            {
                StatusMessage = "Please enter a reason for the adjustment";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Adjusting stock...";

                await _stockService.UpdateStockLevelsAsync(
                    SelectedProduct.Id,
                    AdjustBoxQuantity,
                    AdjustItemQuantity,
                    AdjustmentType,
                    AdjustmentReason,
                    $"MANUAL-{DateTime.Now:yyyyMMddHHmmss}",
                    _currentUser.Id);

                // Refresh stock information
                await LoadProductStockAsync(SelectedProduct);

                StatusMessage = "Stock adjusted successfully";

                // Reset adjustment fields
                AdjustBoxQuantity = 0;
                AdjustItemQuantity = 0;
                AdjustmentReason = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adjusting stock: {ex.Message}";
                _logger.LogError(ex, "Error adjusting stock for product {ProductId}", SelectedProduct.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Performs inventory count for the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void PerformInventory(object parameter)
        {
            if (SelectedProduct == null || SelectedStock == null)
                return;

            // Validate input
            if (AdjustBoxQuantity < 0 || AdjustItemQuantity < 0)
            {
                StatusMessage = "Please enter a valid quantity for inventory count";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Performing inventory count...";

                await _stockService.PerformInventoryCountAsync(
                    SelectedProduct.Id,
                    AdjustBoxQuantity,
                    AdjustItemQuantity,
                    AdjustmentReason,
                    _currentUser.Id);

                // Refresh stock information
                await LoadProductStockAsync(SelectedProduct);

                StatusMessage = "Inventory count completed successfully";

                // Reset adjustment fields
                AdjustBoxQuantity = 0;
                AdjustItemQuantity = 0;
                AdjustmentReason = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error performing inventory count: {ex.Message}";
                _logger.LogError(ex, "Error performing inventory count for product {ProductId}", SelectedProduct.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Converts boxes to items for the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void ConvertBoxesToItems(object parameter)
        {
            if (SelectedProduct == null || SelectedStock == null)
                return;

            // Validate input
            if (AdjustBoxQuantity <= 0)
            {
                StatusMessage = "Please enter a valid number of boxes to convert";
                return;
            }

            if (AdjustBoxQuantity > SelectedStock.BoxQuantity)
            {
                StatusMessage = "Not enough boxes to convert";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Converting boxes to items...";

                await _stockService.ConvertBoxesToItemsAsync(
                    SelectedProduct.Id,
                    AdjustBoxQuantity,
                    _currentUser.Id);

                // Refresh stock information
                await LoadProductStockAsync(SelectedProduct);

                StatusMessage = "Boxes converted to items successfully";

                // Reset adjustment fields
                AdjustBoxQuantity = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error converting boxes to items: {ex.Message}";
                _logger.LogError(ex, "Error converting boxes to items for product {ProductId}", SelectedProduct.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Updates the location code for the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void UpdateLocation(object parameter)
        {
            if (SelectedProduct == null || SelectedStock == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Updating location code...";

                await _stockService.UpdateLocationCodeAsync(
                    SelectedProduct.Id,
                    LocationCode,
                    _currentUser.Id);

                StatusMessage = "Location code updated successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating location code: {ex.Message}";
                _logger.LogError(ex, "Error updating location code for product {ProductId}", SelectedProduct.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads low stock products
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void LoadLowStockProducts(object parameter)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading low stock products...";

                var lowStockProducts = await _productService.GetLowStockProductsAsync();

                Products.Clear();
                foreach (var product in lowStockProducts)
                {
                    Products.Add(product);
                }

                StatusMessage = $"Loaded {Products.Count} low stock products";

                // Clear selected product and stock
                SelectedProduct = null;
                SelectedStock = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading low stock products: {ex.Message}";
                _logger.LogError(ex, "Error loading low stock products");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Prints a stock report
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void PrintStockReport(object parameter)
        {
            // In a real implementation, this would generate and print a stock report
            StatusMessage = "Stock report printing is not implemented yet";
        }

        /// <summary>
        /// Determines if stock can be managed
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if stock can be managed, false otherwise</returns>
        private bool CanManageStock(object parameter)
        {
            return SelectedProduct != null && SelectedStock != null && !IsBusy;
        }

        /// <summary>
        /// Determines if stock can be adjusted
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if stock can be adjusted, false otherwise</returns>
        private bool CanAdjustStock(object parameter)
        {
            return SelectedProduct != null &&
                   SelectedStock != null &&
                   !IsBusy &&
                   (AdjustBoxQuantity > 0 || AdjustItemQuantity > 0) &&
                   !string.IsNullOrWhiteSpace(AdjustmentReason);
        }

        /// <summary>
        /// Determines if boxes can be converted to items
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if boxes can be converted, false otherwise</returns>
        private bool CanConvertBoxes(object parameter)
        {
            return SelectedProduct != null &&
                   SelectedStock != null &&
                   !IsBusy &&
                   AdjustBoxQuantity > 0 &&
                   SelectedStock.BoxQuantity >= AdjustBoxQuantity;
        }
    }
}