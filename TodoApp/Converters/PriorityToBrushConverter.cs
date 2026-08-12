using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TodoApp.Models;

namespace TodoApp.Converters
{
    public class PriorityToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush High = new(Color.FromRgb(0xFF, 0x6B, 0x6B));
        private static readonly SolidColorBrush Medium = new(Color.FromRgb(0xFF, 0xA9, 0x4D));
        private static readonly SolidColorBrush Low = new(Color.FromRgb(0x51, 0xCF, 0x66));

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PriorityLevel level)
            {
                return level switch
                {
                    PriorityLevel.High => High,
                    PriorityLevel.Low => Low,
                    _ => Medium
                };
            }
            return Medium;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
