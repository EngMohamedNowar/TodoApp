using System;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.Views;

namespace TodoApp.ViewModels
{
    public enum PomodoroMode
    {
        Work,
        ShortBreak,
        LongBreak
    }

    public class PomodoroViewModel : ViewModelBase, IDisposable
    {
        private readonly IFocusSessionRepository _sessionRepo;
        private readonly IPomodoroSettingsRepository _settingsRepo;
        private readonly DispatcherTimer _timer;
        private DateTime? _currentSessionStart;
        private int _cyclePosition;
        private bool _disposed;

        private FocusStatsWindow? _statsWindow;

        public Window? OwnerWindow { get; set; }

        public PomodoroViewModel(
            IFocusSessionRepository sessionRepo,
            IPomodoroSettingsRepository settingsRepo)
        {
            _sessionRepo = sessionRepo;
            _settingsRepo = settingsRepo;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            StartPauseCommand = new RelayCommand(_ => ToggleStartPause());
            ResetCommand = new RelayCommand(_ => ResetCurrentSession());
            SkipCommand = new RelayCommand(_ => SkipCurrentMode());
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            OpenStatsCommand = new RelayCommand(_ => OpenStats());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadSettingsAsync();
                _mode = PomodoroMode.Work;
                _remainingSeconds = WorkMinutes * 60;
                OnPropertyChanged(nameof(ModeLabel));
                OnPropertyChanged(nameof(TimeDisplay));
                OnPropertyChanged(nameof(ProgressPercentage));
                await RefreshTodayStatsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize timer settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public int WorkMinutes { get; private set; }
        public int ShortBreakMinutes { get; private set; }
        public int LongBreakMinutes { get; private set; }
        public int SessionsBeforeLongBreak { get; private set; }

        public string SettingsSummary =>
            $"{WorkMinutes} min focus · {ShortBreakMinutes} min short break · {LongBreakMinutes} min long break every {SessionsBeforeLongBreak} sessions";

        private async Task LoadSettingsAsync()
        {
            var settings = await _settingsRepo.GetOrCreateAsync();
            WorkMinutes = Math.Clamp(settings.WorkMinutes, 1, 180);
            ShortBreakMinutes = Math.Clamp(settings.ShortBreakMinutes, 1, 60);
            LongBreakMinutes = Math.Clamp(settings.LongBreakMinutes, 1, 90);
            SessionsBeforeLongBreak = Math.Clamp(settings.SessionsBeforeLongBreak, 1, 12);
        }

        public async Task ApplySettingsAsync(int workMinutes, int shortBreakMinutes, int longBreakMinutes, int sessionsBeforeLongBreak)
        {
            WorkMinutes = Math.Clamp(workMinutes, 1, 180);
            ShortBreakMinutes = Math.Clamp(shortBreakMinutes, 1, 60);
            LongBreakMinutes = Math.Clamp(longBreakMinutes, 1, 90);
            SessionsBeforeLongBreak = Math.Clamp(sessionsBeforeLongBreak, 1, 12);

            try
            {
                await _settingsRepo.SaveAsync(new PomodoroSettingsEntity
                {
                    Id = 1,
                    WorkMinutes = WorkMinutes,
                    ShortBreakMinutes = ShortBreakMinutes,
                    LongBreakMinutes = LongBreakMinutes,
                    SessionsBeforeLongBreak = SessionsBeforeLongBreak
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            OnPropertyChanged(nameof(SettingsSummary));
            _timer.Stop();
            IsRunning = false;
            _currentSessionStart = null;
            RemainingSeconds = GetDurationSecondsForMode(Mode);
        }

        private PomodoroMode _mode;
        public PomodoroMode Mode
        {
            get => _mode;
            private set
            {
                if (SetField(ref _mode, value))
                    OnPropertyChanged(nameof(ModeLabel));
            }
        }

        public string ModeLabel => Mode switch
        {
            PomodoroMode.Work => "FOCUS SESSION",
            PomodoroMode.ShortBreak => "SHORT BREAK",
            PomodoroMode.LongBreak => "LONG BREAK",
            _ => ""
        };

        private int _remainingSeconds;
        public int RemainingSeconds
        {
            get => _remainingSeconds;
            private set
            {
                if (SetField(ref _remainingSeconds, value))
                {
                    OnPropertyChanged(nameof(TimeDisplay));
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public string TimeDisplay
        {
            get
            {
                var seconds = Math.Max(RemainingSeconds, 0);
                return $"{seconds / 60:D2}:{seconds % 60:D2}";
            }
        }

        public double ProgressPercentage
        {
            get
            {
                var total = GetDurationSecondsForMode(Mode);
                if (total <= 0) return 0;
                var elapsed = total - RemainingSeconds;
                return Math.Clamp(elapsed * 100.0 / total, 0, 100);
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetField(ref _isRunning, value))
                    OnPropertyChanged(nameof(StartPauseLabel));
            }
        }

        public string StartPauseLabel => IsRunning ? "Pause" : "Start";

        private int _completedSessions;
        public int CompletedSessions
        {
            get => _completedSessions;
            private set => SetField(ref _completedSessions, value);
        }

        public RelayCommand StartPauseCommand { get; }
        public RelayCommand ResetCommand { get; }
        public RelayCommand SkipCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand OpenStatsCommand { get; }

        private void ToggleStartPause()
        {
            IsRunning = !IsRunning;

            if (IsRunning)
            {
                if (Mode == PomodoroMode.Work && _currentSessionStart == null)
                    _currentSessionStart = DateTime.Now;

                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void ResetCurrentSession()
        {
            _timer.Stop();
            IsRunning = false;
            _currentSessionStart = null;
            RemainingSeconds = GetDurationSecondsForMode(Mode);
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            if (RemainingSeconds > 0)
                RemainingSeconds--;

            if (RemainingSeconds <= 0)
            {
                PlayChime();

                if (Mode == PomodoroMode.Work)
                    await LogCompletedFocusSessionAsync();

                AdvanceToNextMode();
            }
        }

        private async Task LogCompletedFocusSessionAsync()
        {
            try
            {
                var startedAt = _currentSessionStart ?? DateTime.Now.AddMinutes(-WorkMinutes);
                await _sessionRepo.AddAsync(new FocusSession
                {
                    StartedAt = startedAt,
                    CompletedAt = DateTime.Now,
                    DurationMinutes = WorkMinutes
                });
                await _sessionRepo.SaveChangesAsync();
                _currentSessionStart = null;
                await RefreshTodayStatsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to log focus session:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SkipCurrentMode()
        {
            if (Mode == PomodoroMode.Work)
                _ = LogSkippedFocusSessionAsync();

            AdvanceToNextMode();
        }

        private async Task LogSkippedFocusSessionAsync()
        {
            try
            {
                var startedAt = _currentSessionStart ?? DateTime.Now.AddMinutes(-WorkMinutes);
                await _sessionRepo.AddAsync(new FocusSession
                {
                    StartedAt = startedAt,
                    CompletedAt = DateTime.Now,
                    DurationMinutes = WorkMinutes
                });
                await _sessionRepo.SaveChangesAsync();
                _currentSessionStart = null;
                await RefreshTodayStatsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to log skipped session:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AdvanceToNextMode()
        {
            _timer.Stop();
            IsRunning = false;
            _currentSessionStart = null;

            bool autoContinue;

            if (Mode == PomodoroMode.Work)
            {
                _cyclePosition++;
                if (_cyclePosition >= SessionsBeforeLongBreak)
                {
                    _cyclePosition = 0;
                    Mode = PomodoroMode.LongBreak;
                }
                else
                {
                    Mode = PomodoroMode.ShortBreak;
                }
                autoContinue = true;
            }
            else
            {
                Mode = PomodoroMode.Work;
                autoContinue = false;
            }

            RemainingSeconds = GetDurationSecondsForMode(Mode);

            if (autoContinue)
            {
                IsRunning = true;
                _timer.Start();
            }
        }

        private async Task RefreshTodayStatsAsync()
        {
            try
            {
                CompletedSessions = await _sessionRepo.GetTodayCountAsync();
            }
            catch
            {
                CompletedSessions = 0;
            }
        }

        private static void PlayChime()
        {
            try
            {
                Console.Beep(880, 160);
                System.Threading.Thread.Sleep(70);
                Console.Beep(988, 160);
                System.Threading.Thread.Sleep(70);
                Console.Beep(1174, 220);
            }
            catch
            {
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
        }

        private int GetDurationSecondsForMode(PomodoroMode mode) => mode switch
        {
            PomodoroMode.Work => WorkMinutes * 60,
            PomodoroMode.ShortBreak => ShortBreakMinutes * 60,
            PomodoroMode.LongBreak => LongBreakMinutes * 60,
            _ => WorkMinutes * 60
        };

        private void OpenSettings()
        {
            var dialog = new PomodoroSettingsWindow(WorkMinutes, ShortBreakMinutes, LongBreakMinutes, SessionsBeforeLongBreak);
            if (OwnerWindow != null) dialog.Owner = OwnerWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = ApplySettingsAsync(dialog.WorkMinutesResult, dialog.ShortBreakMinutesResult,
                    dialog.LongBreakMinutesResult, dialog.SessionsBeforeLongBreakResult);
            }
        }

        private void OpenStats()
        {
            if (_statsWindow != null)
            {
                _statsWindow.Activate();
                return;
            }

            _statsWindow = new FocusStatsWindow();
            if (OwnerWindow != null) _statsWindow.Owner = OwnerWindow;
            _statsWindow.Closed += (_, _) => _statsWindow = null;
            _statsWindow.Show();
        }

        public void StopTimer()
        {
            _timer.Stop();
            IsRunning = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }
    }
}
