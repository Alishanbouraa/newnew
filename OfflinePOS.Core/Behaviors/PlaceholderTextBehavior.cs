// OfflinePOS.Core/Behaviors/PlaceholderTextBehavior.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OfflinePOS.Core.Behaviors
{
    /// <summary>
    /// Provides placeholder text behavior for TextBox controls
    /// </summary>
    public static class PlaceholderTextBehavior
    {
        /// <summary>
        /// Dependency property for the placeholder text
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(PlaceholderTextBehavior),
                new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

        /// <summary>
        /// Gets the placeholder text value
        /// </summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Placeholder text</returns>
        public static string GetPlaceholderText(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderTextProperty);
        }

        /// <summary>
        /// Sets the placeholder text value
        /// </summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">Placeholder text</param>
        public static void SetPlaceholderText(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                // Remove any previous event handlers
                textBox.GotFocus -= TextBox_GotFocus;
                textBox.LostFocus -= TextBox_LostFocus;
                textBox.TextChanged -= TextBox_TextChanged;

                // Set up for placeholder text
                if (!string.IsNullOrEmpty(e.NewValue?.ToString()))
                {
                    // Add event handlers
                    textBox.GotFocus += TextBox_GotFocus;
                    textBox.LostFocus += TextBox_LostFocus;
                    textBox.TextChanged += TextBox_TextChanged;

                    // Set initial state
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        textBox.Tag = e.NewValue;
                        textBox.Foreground = new SolidColorBrush(Colors.Gray);
                        textBox.Text = e.NewValue.ToString();
                    }
                }
            }
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.Text == GetPlaceholderText(textBox))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = SystemColors.WindowTextBrush;
            }
        }

        private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
                textBox.Text = GetPlaceholderText(textBox);
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.IsFocused && textBox.Text != GetPlaceholderText(textBox))
            {
                textBox.Foreground = SystemColors.WindowTextBrush;
            }
        }
    }
}