using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts an object to Visibility (Visible if not null, Collapsed if null)
    /// </summary>
    public class ObjectToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts object to Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
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
    /// Converts an object to Visibility (Collapsed if not null, Visible if null)
    /// </summary>
    public class InverseObjectToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts object to Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
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