// OfflinePOS.Core/Converters/TotalItemsConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts box quantity to total items using items per box
    /// </summary>
    public class TotalItemsConverter : IValueConverter
    {
        /// <summary>
        /// Converts a box quantity to total items using the items per box parameter
        /// </summary>
        /// <param name="value">Box quantity</param>
        /// <param name="targetType">Target type</param>
        /// <param name="parameter">Items per box</param>
        /// <param name="culture">Culture</param>
        /// <returns>Total items</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int boxQuantity && parameter is int itemsPerBox)
            {
                int totalItems = boxQuantity * itemsPerBox;
                return totalItems.ToString();
            }

            if (value is int boxQty && int.TryParse(parameter?.ToString(), out int itemsPer))
            {
                int total = boxQty * itemsPer;
                return total.ToString();
            }

            return "0";
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}