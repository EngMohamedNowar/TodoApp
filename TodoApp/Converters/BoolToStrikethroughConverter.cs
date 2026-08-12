using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TodoApp.Converters
{
    public class BoolToStrikethroughConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCompleted = value is bool b && b;
            return isCompleted ? TextDecorations.Strikethrough : null!;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
