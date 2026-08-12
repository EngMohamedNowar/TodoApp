using System;
using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Converters
{
    /// <summary>
    /// Converts a completion percentage (0-100) into a pixel width for a
    /// simple custom progress bar. Pass the track's total width as the
    /// converter parameter, e.g. ConverterParameter=190.
    /// </summary>
    public class PercentageToWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = value is double d ? d : 0;
            double maxWidth = 190;

            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                maxWidth = parsed;
            }

            percent = Math.Clamp(percent, 0, 100);
            return maxWidth * (percent / 100.0);
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
