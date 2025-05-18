// OfflinePOS.Core/Converters/StockStatusConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts stock status to a color
    /// </summary>
    public class StockStatusConverter : IValueConverter
    {
        /// <summary>
        /// Converts a stock status to a brush color
        /// </summary>
        /// <param name="value">Stock status</param>
        /// <param name="targetType">Target type</param>
        /// <param name="parameter">Parameter</param>
        /// <param name="culture">Culture</param>
        /// <returns>Brush color based on status</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "available":
                        return new SolidColorBrush(Colors.Green);
                    case "low":
                        return new SolidColorBrush(Colors.Orange);
                    case "out":
                        return new SolidColorBrush(Colors.Red);
                    default:
                        return new SolidColorBrush(Colors.Black);
                }
            }

            return new SolidColorBrush(Colors.Black);
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