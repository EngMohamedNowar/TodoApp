using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TodoApp.Views
{
    public partial class PomodoroSettingsWindow : Window
    {
        public int WorkMinutesResult { get; private set; }

        public int ShortBreakMinutesResult { get; private set; }

        public int LongBreakMinutesResult { get; private set; }

        public int SessionsBeforeLongBreakResult { get; private set; }


        public PomodoroSettingsWindow(
            int workMinutes,
            int shortBreakMinutes,
            int longBreakMinutes,
            int sessionsBeforeLongBreak)
        {
            InitializeComponent();

            WorkBox.Text =
                workMinutes.ToString();

            ShortBreakBox.Text =
                shortBreakMinutes.ToString();

            LongBreakBox.Text =
                longBreakMinutes.ToString();

            SessionsBox.Text =
                sessionsBeforeLongBreak.ToString();
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

            // نطلع للـ TextBox الأصلي اللي الـ Template ده بتاعه
            var textBox = FindParentTextBox(fe);
            if (textBox == null) return;

            if (!int.TryParse(textBox.Text, out int value))
                value = 0;

            value += delta;
            if (value < 1) value = 1; // أقل قيمة مسموحة

            textBox.Text = value.ToString();
            textBox.CaretIndex = textBox.Text.Length;
        }

        private TextBox FindParentTextBox(DependencyObject child)
        {
            while (child != null)
            {
                if (child is TextBox tb) return tb;
                child = VisualTreeHelper.GetParent(child) as DependencyObject
                        ?? LogicalTreeHelper.GetParent(child);
            }
            return null;
        }


        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryParsePositive(
                    WorkBox.Text,
                    1,
                    180,
                    out var work) ||

                !TryParsePositive(
                    ShortBreakBox.Text,
                    1,
                    60,
                    out var shortBreak) ||

                !TryParsePositive(
                    LongBreakBox.Text,
                    1,
                    90,
                    out var longBreak) ||

                !TryParsePositive(
                    SessionsBox.Text,
                    1,
                    12,
                    out var sessions))
            {
                MessageBox.Show(
                    "Please enter valid whole numbers:\n" +
                    "Focus: 1-180 min\n" +
                    "Short break: 1-60 min\n" +
                    "Long break: 1-90 min\n" +
                    "Sessions: 1-12.",
                    "Invalid Value",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            WorkMinutesResult =
                work;

            ShortBreakMinutesResult =
                shortBreak;

            LongBreakMinutesResult =
                longBreak;

            SessionsBeforeLongBreakResult =
                sessions;


            DialogResult = true;
        }


        private static bool TryParsePositive(
            string? text,
            int min,
            int max,
            out int value)
        {
            value = 0;

            if (!int.TryParse(
                    text,
                    out var parsed))
            {
                return false;
            }

            if (parsed < min ||
                parsed > max)
            {
                return false;
            }

            value = parsed;

            return true;
        }


        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}