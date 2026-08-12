using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace TodoApp.Behaviors
{
    /// <summary>
    /// Attach to any FrameworkElement to animate its Width smoothly
    /// whenever the bound value changes, instead of snapping instantly.
    /// Usage: behaviors:SmoothAnimation.TargetWidth="{Binding SomeValue, Converter=...}"
    /// </summary>
    public static class SmoothAnimation
    {
        public static readonly DependencyProperty TargetWidthProperty =
            DependencyProperty.RegisterAttached(
                "TargetWidth",
                typeof(double),
                typeof(SmoothAnimation),
                new PropertyMetadata(0.0, OnTargetWidthChanged));

        public static double GetTargetWidth(DependencyObject obj) => (double)obj.GetValue(TargetWidthProperty);
        public static void SetTargetWidth(DependencyObject obj, double value) => obj.SetValue(TargetWidthProperty, value);

        private static void OnTargetWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element) return;

            var newWidth = (double)e.NewValue;
            if (double.IsNaN(newWidth) || newWidth < 0) return;

            // A Border's Width defaults to Auto (NaN) until it's explicitly set once.
            // DoubleAnimation cannot interpolate from NaN, so seed a real starting
            // value the first time this runs (no animation - avoids an initial jump).
            if (double.IsNaN(element.Width))
            {
                element.Width = 0;
            }

            var currentWidth = double.IsNaN(element.Width) ? 0 : element.Width;

            var animation = new DoubleAnimation
            {
                From = currentWidth,
                To = newWidth,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(FrameworkElement.WidthProperty, animation);
        }

        public static readonly DependencyProperty TargetHeightProperty =
            DependencyProperty.RegisterAttached(
                "TargetHeight",
                typeof(double),
                typeof(SmoothAnimation),
                new PropertyMetadata(0.0, OnTargetHeightChanged));

        public static double GetTargetHeight(DependencyObject obj) => (double)obj.GetValue(TargetHeightProperty);
        public static void SetTargetHeight(DependencyObject obj, double value) => obj.SetValue(TargetHeightProperty, value);

        private static void OnTargetHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element) return;

            var newHeight = (double)e.NewValue;
            if (double.IsNaN(newHeight) || newHeight < 0) return;

            if (double.IsNaN(element.Height))
            {
                element.Height = 0;
            }

            var currentHeight = double.IsNaN(element.Height) ? 0 : element.Height;

            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = newHeight,
                Duration = TimeSpan.FromMilliseconds(380),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }
    }
}
