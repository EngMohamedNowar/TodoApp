using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class PomodoroSettingsWindow : Window
    {
        public int WorkMinutesResult { get; private set; }
        public int ShortBreakMinutesResult { get; private set; }
        public int LongBreakMinutesResult { get; private set; }
        public int SessionsBeforeLongBreakResult { get; private set; }

        private readonly PomodoroSettingsViewModel _viewModel;

        public PomodoroSettingsWindow(
            int workMinutes,
            int shortBreakMinutes,
            int longBreakMinutes,
            int sessionsBeforeLongBreak)
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<PomodoroSettingsViewModel>();
            DataContext = _viewModel;
            _viewModel.Initialize(workMinutes, shortBreakMinutes, longBreakMinutes, sessionsBeforeLongBreak);
        }

        private void NumericUpDown_Increment(object sender, RoutedEventArgs e)
        {
            ChangeValue(sender, 1);
        }

        private void NumericUpDown_Decrement(object sender, RoutedEventArgs e)
        {
            ChangeValue(sender, -1);
        }

        private void ChangeValue(object sender, int delta)
        {
            if (sender is not FrameworkElement fe) return;
            var textBox = FindParentTextBox(fe);
            if (textBox == null) return;

            if (!int.TryParse(textBox.Text, out int value))
                value = 0;

            value += delta;
            if (value < 1) value = 1;

            textBox.Text = value.ToString();
            textBox.CaretIndex = textBox.Text.Length;
        }

        private TextBox? FindParentTextBox(DependencyObject child)
        {
            while (child != null)
            {
                if (child is TextBox tb) return tb;
                child = VisualTreeHelper.GetParent(child) as DependencyObject
                        ?? LogicalTreeHelper.GetParent(child);
            }
            return null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.WorkText = WorkBox.Text;
            _viewModel.ShortBreakText = ShortBreakBox.Text;
            _viewModel.LongBreakText = LongBreakBox.Text;
            _viewModel.SessionsText = SessionsBox.Text;

            if (!_viewModel.Save())
            {
                MessageBox.Show(
                    _viewModel.ErrorMessage,
                    "Invalid Value",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            WorkMinutesResult = _viewModel.WorkMinutesResult;
            ShortBreakMinutesResult = _viewModel.ShortBreakMinutesResult;
            LongBreakMinutesResult = _viewModel.LongBreakMinutesResult;
            SessionsBeforeLongBreakResult = _viewModel.SessionsBeforeLongBreakResult;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
