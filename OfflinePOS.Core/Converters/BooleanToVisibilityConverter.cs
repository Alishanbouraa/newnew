﻿// OfflinePOS.Core/Converters/BooleanToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts a boolean value to a Visibility enumeration value
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to a Visibility enumeration value
        /// </summary>
        /// <param name="value">Boolean value to convert</param>
        /// <param name="targetType">Type of the binding target property</param>
        /// <param name="parameter">Optional converter parameter</param>
        /// <param name="culture">Culture information</param>
        /// <returns>Visibility.Visible if true, Visibility.Collapsed if false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a Visibility enumeration value back to a boolean
        /// </summary>
        /// <param name="value">Visibility enumeration value to convert</param>
        /// <param name="targetType">Type of the binding target property</param>
        /// <param name="parameter">Optional converter parameter</param>
        /// <param name="culture">Culture information</param>
        /// <returns>True if Visible, False if Collapsed</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}