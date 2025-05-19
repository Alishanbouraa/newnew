using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts a string to Visibility based on whether it matches a parameter
    /// </summary>
    public class StringMatchToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts string to Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            string compareValue = parameter as string;

            if (stringValue == null || compareValue == null)
                return Visibility.Collapsed;

            return string.Equals(stringValue, compareValue, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
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
    /// Converts a string to Visibility based on whether it does not match a parameter
    /// </summary>
    public class InverseStringMatchToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts string to Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            string compareValue = parameter as string;

            if (stringValue == null || compareValue == null)
                return Visibility.Visible;

            return !string.Equals(stringValue, compareValue, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
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