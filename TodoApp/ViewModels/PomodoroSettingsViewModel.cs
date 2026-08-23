using System;
using System.Windows;
using TodoApp.ViewModels;

namespace TodoApp.ViewModels
{
    public class PomodoroSettingsViewModel : ViewModelBase
    {
        private string _workText = "25";
        public string WorkText
        {
            get => _workText;
            set => SetField(ref _workText, value);
        }

        private string _shortBreakText = "5";
        public string ShortBreakText
        {
            get => _shortBreakText;
            set => SetField(ref _shortBreakText, value);
        }

        private string _longBreakText = "15";
        public string LongBreakText
        {
            get => _longBreakText;
            set => SetField(ref _longBreakText, value);
        }

        private string _sessionsText = "4";
        public string SessionsText
        {
            get => _sessionsText;
            set => SetField(ref _sessionsText, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public int WorkMinutesResult { get; private set; }
        public int ShortBreakMinutesResult { get; private set; }
        public int LongBreakMinutesResult { get; private set; }
        public int SessionsBeforeLongBreakResult { get; private set; }

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public PomodoroSettingsViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public void Initialize(int workMinutes, int shortBreakMinutes, int longBreakMinutes, int sessionsBeforeLongBreak)
        {
            WorkText = workMinutes.ToString();
            ShortBreakText = shortBreakMinutes.ToString();
            LongBreakText = longBreakMinutes.ToString();
            SessionsText = sessionsBeforeLongBreak.ToString();
        }

        public bool CanSave()
        {
            return TryParsePositive(WorkText, 1, 180, out _) &&
                   TryParsePositive(ShortBreakText, 1, 60, out _) &&
                   TryParsePositive(LongBreakText, 1, 90, out _) &&
                   TryParsePositive(SessionsText, 1, 12, out _);
        }

        public bool Save()
        {
            if (!TryParsePositive(WorkText, 1, 180, out var work) ||
                !TryParsePositive(ShortBreakText, 1, 60, out var shortBreak) ||
                !TryParsePositive(LongBreakText, 1, 90, out var longBreak) ||
                !TryParsePositive(SessionsText, 1, 12, out var sessions))
            {
                ErrorMessage = "Please enter valid whole numbers:\n" +
                    "Focus: 1-180 min\n" +
                    "Short break: 1-60 min\n" +
                    "Long break: 1-90 min\n" +
                    "Sessions: 1-12.";
                return false;
            }

            WorkMinutesResult = work;
            ShortBreakMinutesResult = shortBreak;
            LongBreakMinutesResult = longBreak;
            SessionsBeforeLongBreakResult = sessions;
            ErrorMessage = string.Empty;
            return true;
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        private bool _dialogResult;
        public bool DialogResult
        {
            get => _dialogResult;
            set => SetField(ref _dialogResult, value);
        }

        private static bool TryParsePositive(string? text, int min, int max, out int value)
        {
            value = 0;
            if (!int.TryParse(text, out var parsed) || parsed < min || parsed > max)
                return false;
            value = parsed;
            return true;
        }
    }
}
