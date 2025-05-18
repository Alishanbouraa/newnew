// File: OfflinePOS.Core/Converters/StringToDecimalConverter.cs

using System;
using System.Globalization;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts between string and decimal values with proper empty string handling
    /// </summary>
    public class StringToDecimalConverter : IValueConverter
    {
        /// <summary>
        /// Converts a decimal to a string
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                // Format the decimal value based on the specified format
                string format = parameter as string ?? "0.00";
                return decimalValue.ToString(format, culture);
            }
            return "0.00";
        }

        /// <summary>
        /// Converts a string to a decimal
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // Handle empty string
                if (string.IsNullOrWhiteSpace(stringValue))
                    return 0m;

                // Try to parse the string to a decimal
                if (decimal.TryParse(stringValue, NumberStyles.Any, culture, out decimal result))
                {
                    return result;
                }
            }

            // Return 0 on failure
            return 0m;
        }
    }
}