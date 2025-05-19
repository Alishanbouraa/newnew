// OfflinePOS.Admin/ViewModels/ProductImportExportViewModel.cs
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for product import and export functionality
    /// </summary>
    public class ProductImportExportViewModel : InventoryViewModelBase
    {
        private string _filePath;
        private int _processedCount;

        /// <summary>
        /// Selected file path for import/export
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        /// <summary>
        /// Number of products processed during import/export
        /// </summary>
        public int ProcessedCount
        {
            get => _processedCount;
            set => SetProperty(ref _processedCount, value);
        }

        /// <summary>
        /// Command for downloading CSV template
        /// </summary>
        public ICommand DownloadTemplateCommand { get; }

        /// <summary>
        /// Command for browsing file for import
        /// </summary>
        public ICommand BrowseImportCommand { get; }

        /// <summary>
        /// Command for browsing file location for export
        /// </summary>
        public ICommand BrowseExportCommand { get; }

        /// <summary>
        /// Command for importing products
        /// </summary>
        public ICommand ImportProductsCommand { get; }

        /// <summary>
        /// Command for exporting products
        /// </summary>
        public ICommand ExportProductsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the ProductImportExportViewModel class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        public ProductImportExportViewModel(
          IProductService productService,
          IStockService stockService, // Add this parameter
          ILogger<ProductImportExportViewModel> logger,
          User currentUser)
          : base(productService, stockService, logger, currentUser)
        {
            // Initialize commands
            DownloadTemplateCommand = CreateCommand(DownloadTemplate);
            BrowseImportCommand = CreateCommand(BrowseImport);
            BrowseExportCommand = CreateCommand(BrowseExport);
            ImportProductsCommand = CreateCommand(ImportProducts, CanImport);
            ExportProductsCommand = CreateCommand(ExportProducts, CanExport);
        }

        /// <summary>
        /// Creates and downloads a product import template
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void DownloadTemplate(object parameter)
        {
            try
            {
                // Create a save dialog
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = "ProductImportTemplate.csv",
                    Title = "Save Product Import Template"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Create template content
                    var templateBuilder = new StringBuilder();
                    templateBuilder.AppendLine("Name,Description,CategoryId,BoxBarcode,ItemBarcode,ItemsPerBox," +
                        "BoxPurchasePrice,BoxWholesalePrice,BoxSalePrice,ItemPurchasePrice,ItemWholesalePrice,ItemSalePrice," +
                        "SupplierId,SupplierProductCode,TrackInventory,AllowNegativeInventory,Weight,Dimensions," +
                        "BoxQuantity,ItemQuantity,MinimumBoxLevel,MinimumItemLevel,ReorderBoxQuantity,LocationCode");

                    // Add sample row
                    templateBuilder.AppendLine("Sample Product,Sample description,1,,," +
                        "10,100,120,150,10,12,15,," +
                        ",true,false,0.5,10x5x2," +
                        "5,10,2,5,5,A1-B2");

                    // Write to file
                    File.WriteAllText(saveDialog.FileName, templateBuilder.ToString());

                    StatusMessage = $"Template saved to {saveDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating template: {ex.Message}";
                _logger.LogError(ex, "Error creating import template");
            }
        }

        /// <summary>
        /// Opens a file dialog to select an import file
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void BrowseImport(object parameter)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Select Product Import File"
                };

                if (openDialog.ShowDialog() == true)
                {
                    FilePath = openDialog.FileName;
                    StatusMessage = $"Selected import file: {Path.GetFileName(FilePath)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error selecting file: {ex.Message}";
                _logger.LogError(ex, "Error selecting import file");
            }
        }

        /// <summary>
        /// Opens a file dialog to select an export location
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void BrowseExport(object parameter)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"ProductExport_{DateTime.Now:yyyyMMdd}.csv",
                    Title = "Save Product Export"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    FilePath = saveDialog.FileName;
                    StatusMessage = $"Selected export location: {Path.GetFileName(FilePath)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error selecting file: {ex.Message}";
                _logger.LogError(ex, "Error selecting export file");
            }
        }

        /// <summary>
        /// Imports products from a CSV file
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void ImportProducts(object parameter)
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                StatusMessage = "Please select a valid import file";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Importing products...";

                // Process import
                ProcessedCount = await _productService.ImportProductsFromCsvAsync(FilePath, _currentUser.Id);

                StatusMessage = $"Successfully imported {ProcessedCount} products";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing products: {ex.Message}";
                _logger.LogError(ex, "Error importing products from {FilePath}", FilePath);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Exports products to a CSV file
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void ExportProducts(object parameter)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                StatusMessage = "Please select an export location";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Exporting products...";

                // Process export
                ProcessedCount = await _productService.ExportProductsToCsvAsync(FilePath);

                StatusMessage = $"Successfully exported {ProcessedCount} products";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting products: {ex.Message}";
                _logger.LogError(ex, "Error exporting products to {FilePath}", FilePath);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Determines if products can be imported
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if import operation can be performed, false otherwise</returns>
        private bool CanImport(object parameter)
        {
            return !string.IsNullOrEmpty(FilePath) &&
                   File.Exists(FilePath) &&
                   !IsBusy;
        }

        /// <summary>
        /// Determines if products can be exported
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if export operation can be performed, false otherwise</returns>
        private bool CanExport(object parameter)
        {
            return !string.IsNullOrEmpty(FilePath) &&
                   !IsBusy;
        }
    }
}