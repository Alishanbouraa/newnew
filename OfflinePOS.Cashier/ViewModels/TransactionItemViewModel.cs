// OfflinePOS.Cashier/ViewModels/TransactionItemViewModel.cs
using OfflinePOS.Core.MVVM;
using System;

namespace OfflinePOS.Cashier.ViewModels
{
    /// <summary>
    /// ViewModel for a transaction line item in the cart
    /// </summary>
    public class TransactionItemViewModel : ObservableObject
    {
        private int _productId;
        private string _productName;
        private int _quantity = 1;
        private string _unitType = "Item";
        private decimal _unitPrice;
        private decimal _discountPercentage;
        private decimal _discountAmount;
        private decimal _taxPercentage;
        private decimal _taxAmount;
        private decimal _totalAmount;

        /// <summary>
        /// ID of the product
        /// </summary>
        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        /// <summary>
        /// Name of the product
        /// </summary>
        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        /// <summary>
        /// Quantity of the product
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// Unit type (Box/Item)
        /// </summary>
        public string UnitType
        {
            get => _unitType;
            set => SetProperty(ref _unitType, value);
        }

        /// <summary>
        /// Price per unit
        /// </summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        /// <summary>
        /// Discount percentage for this item
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
        /// Tax percentage for this item
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
        /// Total amount for this item after discounts and taxes
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        /// <summary>
        /// Calculates discounts, taxes, and total for this item
        /// </summary>
        public void CalculateTotals()
        {
            // Calculate line total before discounts and taxes
            decimal lineTotal = Quantity * UnitPrice;

            // Calculate discount
            DiscountAmount = Math.Round(lineTotal * (DiscountPercentage / 100), 2);

            // Calculate tax on discounted amount
            decimal taxableAmount = lineTotal - DiscountAmount;
            TaxAmount = Math.Round(taxableAmount * (TaxPercentage / 100), 2);

            // Calculate total amount
            TotalAmount = lineTotal - DiscountAmount + TaxAmount;
        }
    }
}