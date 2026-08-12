using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TodoApp.ViewModels;

namespace TodoApp.Converters
{
    public class PomodoroModeToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Work = new(Color.FromRgb(0x8B, 0x7C, 0xF6));
        private static readonly SolidColorBrush ShortBreak = new(Color.FromRgb(0x51, 0xCF, 0x66));
        private static readonly SolidColorBrush LongBreakBrush = new(Color.FromRgb(0x4D, 0xAB, 0xF7));

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PomodoroMode mode)
            {
                return mode switch
                {
                    PomodoroMode.Work => Work,
                    PomodoroMode.ShortBreak => ShortBreak,
                    PomodoroMode.LongBreak => LongBreakBrush,
                    _ => Work
                };
            }
            return Work;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
