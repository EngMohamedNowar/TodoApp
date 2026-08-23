using System;
using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Converters
{
    /// <summary>
    /// Multi-value converter that scales a percentage (0-100) against the
    /// ACTUAL rendered width of its progress track, so 100% always fills
    /// the bar exactly to the end regardless of window/layout size.
    ///
    /// Bindings (in order):
    ///   [0] percent        e.g. Binding Path=Percent
    ///   [1] track width    e.g. Binding ElementName=Track Path=ActualWidth
    /// </summary>
    public class PercentOfTrackConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = values.Length > 0 && values[0] is double p ? p : 0;
            double trackWidth = values.Length > 1 && values[1] is double w ? w : 0;

            if (double.IsNaN(trackWidth) || trackWidth <= 0)
                return 0.0;

            if (double.IsNaN(percent))
                percent = 0;

            percent = Math.Clamp(percent, 0, 100);
            return trackWidth * (percent / 100.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
