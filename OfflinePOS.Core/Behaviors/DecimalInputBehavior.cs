// File: OfflinePOS.Core/Behaviors/DecimalInputBehavior.cs

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OfflinePOS.Core.Behaviors
{
    /// <summary>
    /// Behavior for ensuring valid decimal input in TextBox controls
    /// </summary>
    public static class DecimalInputBehavior
    {
        private static readonly Regex _decimalRegex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");

        /// <summary>
        /// Gets whether the behavior is enabled
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DecimalInputBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// Gets the IsEnabled value
        /// </summary>
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets the IsEnabled value
        /// </summary>
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                var isEnabled = (bool)e.NewValue;

                if (isEnabled)
                {
                    textBox.PreviewTextInput += OnPreviewTextInput;
                    textBox.PreviewKeyDown += OnPreviewKeyDown;
                    DataObject.AddPastingHandler(textBox, OnPasting);
                }
                else
                {
                    textBox.PreviewTextInput -= OnPreviewTextInput;
                    textBox.PreviewKeyDown -= OnPreviewKeyDown;
                    DataObject.RemovePastingHandler(textBox, OnPasting);
                }
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var text = textBox.Text;
                var selectionStart = textBox.SelectionStart;
                var selectionLength = textBox.SelectionLength;

                var newText = text.Substring(0, selectionStart) +
                              e.Text +
                              text.Substring(selectionStart + selectionLength);

                e.Handled = !IsValidDecimalInput(newText);
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private static void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));

                if (!IsValidDecimalInput(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsValidDecimalInput(string text)
        {
            return _decimalRegex.IsMatch(text);
        }
    }
}