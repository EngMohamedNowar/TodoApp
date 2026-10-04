using System;
using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Converters
{
    public class FilterActiveTagConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
            => string.Equals(
                value?.ToString(),
                parameter?.ToString(),
                StringComparison.OrdinalIgnoreCase)
                ? "Active"
                : string.Empty;

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
