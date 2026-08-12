using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Threading;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Views;

namespace TodoApp.ViewModels
{
    public enum PomodoroMode
    {
        Work,
        ShortBreak,
        LongBreak
    }

    /// <summary>
    /// Drives a Pomodoro cycle with user-customizable durations, logging
    /// every completed focus session to the database for history/stats.
    /// "Today's" counters are simply recomputed from that log filtered to
    /// today's date, so they naturally reset each day while the
    /// underlying history is preserved until the user clears it.
    /// </summary>
    public class PomodoroViewModel : ViewModelBase
    {
        private readonly TodoDbContext _db;
        private readonly DispatcherTimer _timer;
        private DateTime? _currentSessionStart;
        private int _cyclePosition;

        private FocusStatsWindow? _statsWindow;

        /// <summary>Set by the hosting PomodoroWindow so child dialogs can be owned correctly.</summary>
        public Window? OwnerWindow { get; set; }
        private void LogSkippedFocusSession()
        {
            var startedAt =
                _currentSessionStart
                ?? DateTime.Now.AddMinutes(-WorkMinutes);

            _db.FocusSessions.Add(new FocusSession
            {
                StartedAt = startedAt,
                CompletedAt = DateTime.Now,
                DurationMinutes = WorkMinutes
            });

            _db.SaveChanges();

            _currentSessionStart = null;

            RefreshTodayStats();
        }
        private void SkipCurrentMode()
        {
            // Skip a Focus session = count it as completed
            if (Mode == PomodoroMode.Work)
            {
                LogSkippedFocusSession();
            }

            AdvanceToNextMode();
        }

        public PomodoroViewModel()
        {
            _db = new TodoDbContext();
            _db.EnsureSchema();

            LoadSettings();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            _mode = PomodoroMode.Work;
            _remainingSeconds = WorkMinutes * 60;

            StartPauseCommand = new RelayCommand(_ => ToggleStartPause());
            ResetCommand = new RelayCommand(_ => ResetCurrentSession());
            SkipCommand = new RelayCommand(_ => SkipCurrentMode());
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            OpenStatsCommand = new RelayCommand(_ => OpenStats());

            RefreshTodayStats();
        }

        // ===================== Durations (persisted, user-editable) =====================

        public int WorkMinutes { get; private set; }
        public int ShortBreakMinutes { get; private set; }
        public int LongBreakMinutes { get; private set; }
        public int SessionsBeforeLongBreak { get; private set; }

        public string SettingsSummary =>
            $"{WorkMinutes} min focus · {ShortBreakMinutes} min short break · {LongBreakMinutes} min long break every {SessionsBeforeLongBreak} sessions";

        private void LoadSettings()
        {
            var settings = _db.PomodoroSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null)
            {
                settings = new PomodoroSettingsEntity { Id = 1 };
                _db.PomodoroSettings.Add(settings);
                _db.SaveChanges();
            }

            WorkMinutes = Math.Clamp(settings.WorkMinutes, 1, 180);
            ShortBreakMinutes = Math.Clamp(settings.ShortBreakMinutes, 1, 60);
            LongBreakMinutes = Math.Clamp(settings.LongBreakMinutes, 1, 90);
            SessionsBeforeLongBreak = Math.Clamp(settings.SessionsBeforeLongBreak, 1, 12);
        }

        /// <summary>Applies and persists new durations, then resets the current session to reflect them.</summary>
        public void ApplySettings(int workMinutes, int shortBreakMinutes, int longBreakMinutes, int sessionsBeforeLongBreak)
        {
            WorkMinutes = Math.Clamp(workMinutes, 1, 180);
            ShortBreakMinutes = Math.Clamp(shortBreakMinutes, 1, 60);
            LongBreakMinutes = Math.Clamp(longBreakMinutes, 1, 90);
            SessionsBeforeLongBreak = Math.Clamp(sessionsBeforeLongBreak, 1, 12);

            var settings = _db.PomodoroSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null)
            {
                settings = new PomodoroSettingsEntity { Id = 1 };
                _db.PomodoroSettings.Add(settings);
            }
            settings.WorkMinutes = WorkMinutes;
            settings.ShortBreakMinutes = ShortBreakMinutes;
            settings.LongBreakMinutes = LongBreakMinutes;
            settings.SessionsBeforeLongBreak = SessionsBeforeLongBreak;

            OnPropertyChanged(nameof(SettingsSummary));

            // Reset the current session so the new duration takes effect immediately.
            _timer.Stop();
            IsRunning = false;
            _currentSessionStart = null;
            RemainingSeconds = GetDurationSecondsForMode(Mode);
            _db.SaveChanges();
        }

        // ===================== Live timer state =====================

        private PomodoroMode _mode;
        public PomodoroMode Mode
        {
            get => _mode;
            private set
            {
                if (SetField(ref _mode, value))
                {
                    OnPropertyChanged(nameof(ModeLabel));
                }
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
                {
                    OnPropertyChanged(nameof(StartPauseLabel));
                }
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
                {
                    _currentSessionStart = DateTime.Now;
                }
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

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (RemainingSeconds > 0)
            {
                RemainingSeconds--;
            }

            if (RemainingSeconds <= 0)
            {
                PlayChime();

                if (Mode == PomodoroMode.Work)
                {
                    LogCompletedFocusSession();
                }

                AdvanceToNextMode();
            }
        }

        private void LogCompletedFocusSession()
        {
            var startedAt = _currentSessionStart ?? DateTime.Now.AddMinutes(-WorkMinutes);
            _db.FocusSessions.Add(new FocusSession
            {
                StartedAt = startedAt,
                CompletedAt = DateTime.Now,
                DurationMinutes = WorkMinutes
            });
            _db.SaveChanges();
            _currentSessionStart = null;

            RefreshTodayStats();
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

                // Breaks start automatically so the app doesn't just sit
                // paused after a focus session - only returning to a focus
                // session requires the person to deliberately press Start.
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

        private void RefreshTodayStats()
        {
            var today = DateTime.Today;
            CompletedSessions = _db.FocusSessions.Count(f => f.StartedAt >= today && f.StartedAt < today.AddDays(1));
        }

        private static void PlayChime()
        {
            // Console.Beep talks to the system beep driver directly and does
            // NOT depend on the user's Windows sound scheme, unlike
            // SystemSounds.Play() - so it stays audible even if the person
            // has "No Sounds" selected or no sound mapped to an event.
            // Run on a background thread since Beep blocks for its duration.
            System.Threading.Tasks.Task.Run(() =>
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
                    // Fall back to the system notification sound if Beep is unavailable
                    // (e.g. certain sandboxed/remote-session environments).
                    try { SystemSounds.Asterisk.Play(); } catch { /* no audio device available */ }
                }
            });
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
                ApplySettings(dialog.WorkMinutesResult, dialog.ShortBreakMinutesResult,
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

        /// <summary>Stops the underlying timer, e.g. when the timer window is closed.</summary>
        public void StopTimer()
        {
            _timer.Stop();
            IsRunning = false;
        }
    }
}
