// OfflinePOS.Core/Converters/PaymentButtonVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OfflinePOS.Core.Converters
{
    /// <summary>
    /// Converts invoice status to payment button visibility
    /// </summary>
    public class PaymentButtonVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts an invoice status to a Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                // Only show the payment button for pending or partially paid invoices
                if (status == "Pending" || status == "PartiallyPaid")
                {
                    return Visibility.Visible;
                }
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
}