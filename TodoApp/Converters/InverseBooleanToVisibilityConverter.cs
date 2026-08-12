using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters
{
    /// <summary>Visible when the bound boolean is false, Collapsed when true.</summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = value is bool b && b;
            return flag ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
