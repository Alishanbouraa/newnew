// OfflinePOS.Admin/ViewModels/BarcodeManagementViewModel.cs
using Microsoft.Extensions.Logging;
using OfflinePOS.Core.Models;
using OfflinePOS.Core.MVVM;
using OfflinePOS.Core.Services;
using OfflinePOS.Core.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OfflinePOS.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for barcode generation and management
    /// </summary>
    public class BarcodeManagementViewModel : InventoryViewModelBase
    {
        private string _generatedBarcode;
        private string _barcodeType;
        private bool _showBoxBarcode;
        private bool _showItemBarcode;

        /// <summary>
        /// Generated barcode value
        /// </summary>
        public string GeneratedBarcode
        {
            get => _generatedBarcode;
            set => SetProperty(ref _generatedBarcode, value);
        }

        /// <summary>
        /// Type of barcode to generate (Box/Item)
        /// </summary>
        public string BarcodeType
        {
            get => _barcodeType;
            set
            {
                if (SetProperty(ref _barcodeType, value))
                {
                    ShowBoxBarcode = value == "Box";
                    ShowItemBarcode = value == "Item";
                }
            }
        }

        /// <summary>
        /// Flag indicating if box barcode should be shown
        /// </summary>
        public bool ShowBoxBarcode
        {
            get => _showBoxBarcode;
            set => SetProperty(ref _showBoxBarcode, value);
        }

        /// <summary>
        /// Flag indicating if item barcode should be shown
        /// </summary>
        public bool ShowItemBarcode
        {
            get => _showItemBarcode;
            set => SetProperty(ref _showItemBarcode, value);
        }
        // Add this method after the constructor
        /// <summary>
        /// Loads data from repositories
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task LoadProductsPublicAsync()
        {
            await LoadDataAsync();
        }
        /// <summary>
        /// List of available barcode types
        /// </summary>
        public ObservableCollection<string> BarcodeTypes { get; } = new ObservableCollection<string>
        {
            "Box",
            "Item"
        };

        /// <summary>
        /// Command for generating a barcode
        /// </summary>
        public ICommand GenerateBarcodeCommand { get; }

        /// <summary>
        /// Command for saving a barcode to the product
        /// </summary>
        public ICommand SaveBarcodeCommand { get; }

        /// <summary>
        /// Command for validating a barcode
        /// </summary>
        public ICommand ValidateBarcodeCommand { get; }

        /// <summary>
        /// Command for printing a barcode
        /// </summary>
        public ICommand PrintBarcodeCommand { get; }

        /// <summary>
        /// Initializes a new instance of the BarcodeManagementViewModel class
        /// </summary>
        /// <param name="productService">Product service</param>
        /// <param name="stockService">Stock service</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Current user</param>
        public BarcodeManagementViewModel(
            IProductService productService,
            IStockService stockService,
            ILogger<BarcodeManagementViewModel> logger,
            User currentUser)
            : base(productService, stockService, logger, currentUser)
        {
            // Initialize commands
            GenerateBarcodeCommand = CreateCommand(GenerateBarcode, CanGenerateBarcode);
            SaveBarcodeCommand = CreateCommand(SaveBarcode, CanSaveBarcode);
            ValidateBarcodeCommand = CreateCommand(ValidateBarcode);
            PrintBarcodeCommand = CreateCommand(PrintBarcode, CanPrintBarcode);

            // Set default barcode type
            BarcodeType = "Box";
        }

        /// <summary>
        /// Generates a barcode for the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void GenerateBarcode(object parameter)
        {
            if (SelectedProduct == null)
                return;

            try
            {
                GeneratedBarcode = BarcodeUtility.GenerateEAN13(SelectedProduct.Id);
                StatusMessage = "Barcode generated successfully";
            }
            catch (Exception ex)
            {
                GeneratedBarcode = string.Empty;
                StatusMessage = $"Error generating barcode: {ex.Message}";
                _logger.LogError(ex, "Error generating barcode for product {ProductId}", SelectedProduct.Id);
            }
        }

        /// <summary>
        /// Saves the generated barcode to the selected product
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void SaveBarcode(object parameter)
        {
            if (SelectedProduct == null || string.IsNullOrEmpty(GeneratedBarcode))
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Saving barcode...";

                // Update the product with the new barcode
                if (BarcodeType == "Box")
                {
                    SelectedProduct.BoxBarcode = GeneratedBarcode;
                }
                else
                {
                    SelectedProduct.ItemBarcode = GeneratedBarcode;
                }

                await _productService.UpdateProductAsync(SelectedProduct);

                StatusMessage = "Barcode saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving barcode: {ex.Message}";
                _logger.LogError(ex, "Error saving barcode for product {ProductId}", SelectedProduct.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Validates the generated barcode
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void ValidateBarcode(object parameter)
        {
            if (string.IsNullOrEmpty(GeneratedBarcode))
            {
                StatusMessage = "Please generate a barcode first";
                return;
            }

            try
            {
                bool isValid = BarcodeUtility.ValidateEAN13(GeneratedBarcode);

                if (isValid)
                {
                    StatusMessage = "Barcode is valid";
                }
                else
                {
                    StatusMessage = "Barcode is invalid";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error validating barcode: {ex.Message}";
                _logger.LogError(ex, "Error validating barcode");
            }
        }

        /// <summary>
        /// Prints the generated barcode
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private void PrintBarcode(object parameter)
        {
            // In a real implementation, this would send the barcode to a printer
            StatusMessage = "Barcode printing is not implemented yet";
        }

        /// <summary>
        /// Determines if a barcode can be generated
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if barcode can be generated, false otherwise</returns>
        private bool CanGenerateBarcode(object parameter)
        {
            return SelectedProduct != null && !IsBusy;
        }

        /// <summary>
        /// Determines if a barcode can be saved
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if barcode can be saved, false otherwise</returns>
        private bool CanSaveBarcode(object parameter)
        {
            return SelectedProduct != null &&
                   !string.IsNullOrEmpty(GeneratedBarcode) &&
                   !IsBusy;
        }

        /// <summary>
        /// Determines if a barcode can be printed
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if barcode can be printed, false otherwise</returns>
        private bool CanPrintBarcode(object parameter)
        {
            return !string.IsNullOrEmpty(GeneratedBarcode) && !IsBusy;
        }
    }
}