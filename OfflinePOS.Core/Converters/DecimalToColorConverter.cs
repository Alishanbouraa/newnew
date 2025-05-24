// OfflinePOS.Core/Converters/DecimalToColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts a decimal value to a color based on sign (positive, negative, or zero)
    /// </summary>
    public class DecimalToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a decimal value to a SolidColorBrush:
        /// - Positive: Red
        /// - Zero: Green
        /// - Negative: Red
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue > 0)
                {
                    return new SolidColorBrush(Colors.Red); // Still owes money
                }
                else if (decimalValue < 0)
                {
                    return new SolidColorBrush(Colors.Red); // Negative balance (shouldn't happen normally)
                }
                else
                {
                    return new SolidColorBrush(Colors.Green); // Fully paid
                }
            }

            return new SolidColorBrush(Colors.Black); // Default color
        }

        /// <summary>
        /// Not implemented - conversion back is not needed
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}