using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace TodoApp.Services
{
    /// <summary>
    /// Applies accent color presets to the live application resources and
    /// persists the selection via SettingsStore.
    /// </summary>
    public static class ThemeService
    {
        public sealed record AccentPreset(string Name, string Hex);

        public static readonly IReadOnlyList<AccentPreset> Presets = new[]
        {
            new AccentPreset("Violet", "#8B7CF6"),
            new AccentPreset("Blue",   "#4DABF7"),
            new AccentPreset("Green",  "#51CF66"),
            new AccentPreset("Orange", "#FFA94D"),
            new AccentPreset("Pink",   "#F783AC"),
            new AccentPreset("Red",    "#FF6B6B")
        };

        /// <summary>Accent in effect right now; converters and view models read it.</summary>
        public static Color CurrentAccentColor { get; private set; } = Color.FromRgb(0x8B, 0x7C, 0xF6);

        /// <summary>Raised after every accent change so bindings that resolve once can refresh.</summary>
        public static event EventHandler? AccentChanged;

        public static void ApplyAccent(string hex, bool persist = true)
        {
            if (!TryParseHex(hex, out var baseColor)) return;

            CurrentAccentColor = baseColor;

            var dark = ChangeBrightness(baseColor, -0.22);
            var soft = Color.FromArgb(0x2A,
                (byte)(baseColor.R * 0.55), (byte)(baseColor.G * 0.55), (byte)(baseColor.B * 0.55));

            SetBrush("AccentBrush", baseColor);
            SetBrush("AccentBrushDark", dark);
            SetBrush("AccentSoftBrush", soft);
            SetBrush("AccentHoverBrush", ChangeBrightness(baseColor, 0.18));
            SetBrush("AccentPressedBrush", ChangeBrightness(baseColor, -0.15));

            SetColor("AccentColor", baseColor);
            SetColor("AccentColorDark", dark);

            if (persist)
                Save(hex);

            AccentChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void SetBrush(string key, Color color)
        {
            var resources = Application.Current.Resources;

            // XAML-declared brushes are frozen, so mutation is only possible
            // when nothing has frozen the entry yet. Either way the entry is
            // refreshed in place; views reference these keys with
            // {DynamicResource ...} so they re-resolve the new value live.
            if (resources[key] is SolidColorBrush existing && !existing.IsFrozen)
            {
                existing.Color = color;
                return;
            }

            var replacement = new SolidColorBrush(color);

            if (resources.Contains(key))
                resources[key] = replacement;
            else
                resources.Add(key, replacement);
        }

        private static void SetColor(string key, Color color)
        {
            var resources = Application.Current.Resources;

            if (resources.Contains(key))
                resources[key] = color;
            else
                resources.Add(key, color);
        }

        private static void Save(string hex)
        {
            try
            {
                var prefs = SettingsStore.Load();
                prefs.AccentColor = hex;
                SettingsStore.Save(prefs);
            }
            catch
            {
                // persistence is best-effort
            }
        }

        private static bool TryParseHex(string hex, out Color color)
        {
            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex);
                return true;
            }
            catch
            {
                color = Colors.Transparent;
                return false;
            }
        }

        private static Color ChangeBrightness(Color color, double factor)
        {
            byte Adjust(int channel)
            {
                var value = factor < 0
                    ? channel * (1 + factor)
                    : channel + (255 - channel) * factor;
                return (byte)Math.Clamp(value, 0, 255);
            }

            return Color.FromRgb(Adjust(color.R), Adjust(color.G), Adjust(color.B));
        }
    }
}
