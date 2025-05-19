// OfflinePOS.Core/Converters/StringMatchToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts a string match to a Visibility
    /// </summary>
    public class StringMatchToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a string match to a Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string targetValue)
            {
                return string.Equals(stringValue, targetValue, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
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
    /// Converts a non-match of strings to a Visibility
    /// </summary>
    public class InverseStringMatchToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a non-match of strings to a Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string targetValue)
            {
                return !string.Equals(stringValue, targetValue, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Visible;
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