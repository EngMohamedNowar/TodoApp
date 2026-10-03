using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TodoApp.Helpers
{
    /// <summary>
    /// Renders a window's native title bar in dark mode so it matches the dark UI.
    /// Set <c>helpers:WindowTheme.DarkTitleBar="True"</c> on a Window.
    /// </summary>
    public static class WindowTheme
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

        public static readonly DependencyProperty DarkTitleBarProperty =
            DependencyProperty.RegisterAttached(
                "DarkTitleBar",
                typeof(bool),
                typeof(WindowTheme),
                new PropertyMetadata(false, OnDarkTitleBarChanged));

        public static void SetDarkTitleBar(DependencyObject element, bool value)
            => element.SetValue(DarkTitleBarProperty, value);

        public static bool GetDarkTitleBar(DependencyObject element)
            => (bool)element.GetValue(DarkTitleBarProperty);

        private static void OnDarkTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window || e.NewValue is not true)
                return;

            if (PresentationSource.FromVisual(window) is null)
                window.SourceInitialized += (_, _) => ApplyDarkTitleBar(window);
            else
                ApplyDarkTitleBar(window);
        }

        private static void ApplyDarkTitleBar(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var dark = 1;
            if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref dark, sizeof(int));
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
    }
}
