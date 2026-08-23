using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TodoApp.Services;

namespace TodoApp.Views
{
    public partial class ThemePickerWindow : Window
    {
        public ThemePickerWindow()
        {
            InitializeComponent();

            var current = SettingsStore.Load().AccentColor;

            foreach (var preset in ThemeService.Presets)
            {
                var swatch = CreateSwatch(preset, preset.Hex == current);
                SwatchesPanel.Children.Add(swatch);
            }
        }

        private Border CreateSwatch(ThemeService.AccentPreset preset, bool isSelected)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset.Hex));

            var button = new Button
            {
                Width = 52,
                Height = 52,
                Margin = new Thickness(0, 0, 10, 10),
                Background = brush,
                BorderThickness = new Thickness(isSelected ? 3 : 1),
                BorderBrush = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x2B, 0x2D, 0x3A)),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = preset.Name
            };

            button.Click += (_, _) =>
            {
                ThemeService.ApplyAccent(preset.Hex);

                foreach (Border other in SwatchesPanel.Children)
                    other.BorderThickness = new Thickness(1);

                button.BorderThickness = new Thickness(3);
            };

            return new Border
            {
                Child = button,
                Tag = preset
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
