using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TodoApp.Helpers
{
    /// <summary>
    /// Grows a multi-line <see cref="TextBox"/> so it is always exactly as tall as the
    /// text it holds, clamped to shared minimum/maximum bounds. Every dialog uses this,
    /// so its description box has the same size everywhere and never scrolls a single
    /// line of text that would fit.
    /// </summary>
    public static class TextBoxAutoGrow
    {
        public const double MinLines = 4;
        public const double MaxLines = 16;

        /// <summary>A pixel of slack so sub-pixel rounding cannot open a scrollbar.</summary>
        private const double Slack = 1;

        public static void Resize(TextBox box)
        {
            if (box == null) return;

            var width = box.ActualWidth;
            if (width <= 0) return;

            var padding = box.Padding;
            var border = box.BorderThickness;

            // The TextBox template applies Padding twice (outer Border plus the inner
            // ScrollViewer margin), so the usable text area is inset by 2*Padding plus
            // a single BorderThickness on every side.
            var contentWidth = width - 2 * (padding.Left + padding.Right) - (border.Left + border.Right);
            if (contentWidth <= 1) return;

            var text = string.IsNullOrEmpty(box.Text) ? "Ay" : box.Text;
            var typeface = new Typeface(box.FontFamily, box.FontStyle, box.FontWeight, box.FontStretch);
            var dpi = VisualTreeHelper.GetDpi(box).PixelsPerDip;

            var oneLine = Measure("Ay", typeface, box.FontSize, dpi, double.PositiveInfinity);
            if (oneLine <= 0) return;

            var twoLines = Measure("Ay\nAy", typeface, box.FontSize, dpi, double.PositiveInfinity);
            var lineAdvance = twoLines - oneLine;
            if (lineAdvance <= 0) lineAdvance = oneLine;

            var needed = Measure(text, typeface, box.FontSize, dpi, contentWidth) + Slack;
            if (needed <= 0) needed = oneLine;

            var minHeight = (MinLines - 1) * lineAdvance + oneLine;
            var maxHeight = (MaxLines - 1) * lineAdvance + oneLine;

            var height = Math.Min(maxHeight, Math.Max(minHeight, needed));

            box.Height = height
                         + 2 * (padding.Top + padding.Bottom)
                         + (border.Top + border.Bottom);
        }

        private static double Measure(string text, Typeface typeface, double fontSize, double dpi, double maxWidth)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Transparent,
                dpi);

            if (!double.IsPositiveInfinity(maxWidth))
            {
                formatted.MaxTextWidth = maxWidth;
            }

            return formatted.Height;
        }
    }
}
