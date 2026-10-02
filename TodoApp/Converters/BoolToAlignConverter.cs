using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters
{
    /// <summary>
    /// Converts a boolean (true for the user's own messages) into a
    /// HorizontalAlignment so chat bubbles align left (assistant) or right (user).
    /// </summary>
    public class BoolToAlignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
