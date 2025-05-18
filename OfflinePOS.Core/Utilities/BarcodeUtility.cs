// OfflinePOS.Core/Utilities/BarcodeUtility.cs
using System;
using System.Text;

namespace OfflinePOS.Core.Utilities
{
    /// <summary>
    /// Utility class for barcode generation and validation
    /// </summary>
    public static class BarcodeUtility
    {
        /// <summary>
        /// Generates a unique EAN-13 barcode
        /// </summary>
        /// <param name="productId">Product ID to use in the barcode</param>
        /// <returns>Valid EAN-13 barcode</returns>
        public static string GenerateEAN13(int productId)
        {
            // Format: CCCPPPPPPPPE where C=Company Prefix (3 digits), P=Product Code (8 digits), E=Check digit (1 digit)
            // Company prefix hardcoded for demonstration - in production this would be configurable
            string companyPrefix = "500";

            // Use product ID padded to 8 digits
            string productCode = productId.ToString().PadLeft(8, '0');
            if (productCode.Length > 8)
                productCode = productCode.Substring(productCode.Length - 8, 8);

            string barcodeWithoutChecksum = companyPrefix + productCode;
            string checkDigit = CalculateEAN13CheckDigit(barcodeWithoutChecksum);

            return barcodeWithoutChecksum + checkDigit;
        }

        /// <summary>
        /// Validates an EAN-13 barcode
        /// </summary>
        /// <param name="barcode">Barcode to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateEAN13(string barcode)
        {
            if (string.IsNullOrEmpty(barcode) || barcode.Length != 13 || !long.TryParse(barcode, out _))
                return false;

            string barcodeWithoutChecksum = barcode.Substring(0, 12);
            string expectedCheckDigit = CalculateEAN13CheckDigit(barcodeWithoutChecksum);
            string actualCheckDigit = barcode.Substring(12, 1);

            return expectedCheckDigit == actualCheckDigit;
        }

        /// <summary>
        /// Calculates the check digit for an EAN-13 barcode
        /// </summary>
        /// <param name="barcodeWithoutChecksum">First 12 digits of the barcode</param>
        /// <returns>Check digit as string</returns>
        private static string CalculateEAN13CheckDigit(string barcodeWithoutChecksum)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(barcodeWithoutChecksum[i].ToString());
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit.ToString();
        }

        /// <summary>
        /// Generates a random unique SKU 
        /// </summary>
        /// <param name="categoryCode">Category code (2-3 letters)</param>
        /// <returns>Unique SKU</returns>
        public static string GenerateSKU(string categoryCode)
        {
            if (string.IsNullOrEmpty(categoryCode))
                categoryCode = "GEN";

            categoryCode = categoryCode.ToUpper().Substring(0, Math.Min(3, categoryCode.Length));
            string timestamp = DateTime.Now.ToString("yyMMddHHmm");
            string random = new Random().Next(100, 999).ToString();

            return $"{categoryCode}-{timestamp}-{random}";
        }
    }
}