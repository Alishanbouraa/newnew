// OfflinePOS.Core/Converters/StatusToColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts transaction status to a brush color
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a status to a brush
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "completed":
                        return new SolidColorBrush(Colors.Green);
                    case "pending":
                        return new SolidColorBrush(Colors.Orange);
                    case "cancelled":
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

    /// <summary>
    /// Converts a balance amount to a color
    /// </summary>
    public class BalanceToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a balance amount to a brush
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal balance)
            {
                if (balance > 0)
                    return new SolidColorBrush(Colors.Red);
                else
                    return new SolidColorBrush(Colors.Green);
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