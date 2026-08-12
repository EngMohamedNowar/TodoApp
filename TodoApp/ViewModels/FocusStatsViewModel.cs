using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using TodoApp.Data;

namespace TodoApp.ViewModels
{
    public class DayBarViewModel : ViewModelBase
    {
        public string DayLabel { get; set; } = "";
        public double Hours { get; set; }
        public string HoursLabel => Hours <= 0 ? "" : Hours.ToString("0.#", CultureInfo.InvariantCulture) + "h";
        public double BarHeight { get; set; }
        public bool IsToday { get; set; }
    }

    public class FocusSessionRowViewModel
    {
        public string DateLabel { get; set; } = "";
        public string TimeRangeLabel { get; set; } = "";
        public string DurationLabel { get; set; } = "";
    }

    public class FocusStatsViewModel : ViewModelBase
    {
        private readonly TodoDbContext _db;
        private const double ChartMaxHeight = 130;

        public FocusStatsViewModel()
        {
            _db = new TodoDbContext();
            _db.EnsureSchema();

            PrevWeekCommand = new RelayCommand(_ => ChangeWeek(-1));
            NextWeekCommand = new RelayCommand(_ => ChangeWeek(1), _ => _weekOffset < 0);
            ClearHistoryCommand = new RelayCommand(_ => ClearHistory());
            ShowSummaryTabCommand = new RelayCommand(_ => IsHistoryTabActive = false);
            ShowHistoryTabCommand = new RelayCommand(_ => IsHistoryTabActive = true);

            RefreshAll();
        }

        private bool _isHistoryTabActive;
        public bool IsHistoryTabActive
        {
            get => _isHistoryTabActive;
            set => SetField(ref _isHistoryTabActive, value);
        }

        public RelayCommand ShowSummaryTabCommand { get; }
        public RelayCommand ShowHistoryTabCommand { get; }

        public ObservableCollection<DayBarViewModel> WeekBars { get; } = new();
        public ObservableCollection<FocusSessionRowViewModel> HistoryRows { get; } = new();

        public RelayCommand PrevWeekCommand { get; }
        public RelayCommand NextWeekCommand { get; }
        public RelayCommand ClearHistoryCommand { get; }

        private double _hoursFocusedToday;
        public double HoursFocusedToday
        {
            get => _hoursFocusedToday;
            private set => SetField(ref _hoursFocusedToday, value);
        }

        private int _daysAccessed;
        public int DaysAccessed
        {
            get => _daysAccessed;
            private set => SetField(ref _daysAccessed, value);
        }

        private int _dayStreak;
        public int DayStreak
        {
            get => _dayStreak;
            private set => SetField(ref _dayStreak, value);
        }

        private string _weekRangeLabel = "";
        public string WeekRangeLabel
        {
            get => _weekRangeLabel;
            private set => SetField(ref _weekRangeLabel, value);
        }

        private bool _hasHistory;
        public bool HasHistory
        {
            get => _hasHistory;
            private set => SetField(ref _hasHistory, value);
        }

        private int _weekOffset;

        private void ChangeWeek(int delta)
        {
            var newOffset = _weekOffset + delta;
            if (newOffset > 0) return;
            _weekOffset = newOffset;
            LoadWeekBars();
            NextWeekCommand.RaiseCanExecuteChanged();
        }

        private void RefreshAll()
        {
            LoadSummary();
            LoadWeekBars();
            LoadHistory();
        }

        private void LoadSummary()
        {
            // Summary always counts ALL sessions (hidden or not) - Clear History
            // only soft-deletes rows from the History list, never from these stats.
            var today = DateTime.Today;

            HoursFocusedToday = Math.Round(
                _db.FocusSessions.Where(f => f.StartedAt >= today && f.StartedAt < today.AddDays(1))
                    .Sum(f => (double?)f.DurationMinutes) / 60.0 ?? 0, 1);

            var distinctDates = _db.FocusSessions
                .Select(f => f.StartedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            DaysAccessed = distinctDates.Count;

            var cursor = today;
            if (!distinctDates.Contains(cursor)) cursor = cursor.AddDays(-1);
            var streak = 0;
            while (distinctDates.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
            DayStreak = streak;

            HasHistory = distinctDates.Count > 0;
        }

        private void LoadWeekBars()
        {
            // Also counts ALL sessions - same reason as LoadSummary.
            WeekBars.Clear();

            var today = DateTime.Today;
            var mondayThisWeek = today.AddDays(-(int)today.DayOfWeek + (today.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
            var weekStart = mondayThisWeek.AddDays(_weekOffset * 7);
            var weekEnd = weekStart.AddDays(7);

            WeekRangeLabel = _weekOffset == 0
                ? "This Week"
                : $"{weekStart:MMM d} - {weekEnd.AddDays(-1):MMM d}";

            var dailyHours = new double[7];
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var minutes = _db.FocusSessions
                    .Where(f => f.StartedAt >= day && f.StartedAt < day.AddDays(1))
                    .Sum(f => (double?)f.DurationMinutes) ?? 0;
                dailyHours[i] = minutes / 60.0;
            }

            var maxHours = Math.Max(dailyHours.Max(), 1.0);

            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var hours = dailyHours[i];
                WeekBars.Add(new DayBarViewModel
                {
                    DayLabel = day.ToString("ddd", CultureInfo.InvariantCulture),
                    Hours = Math.Round(hours, 2),
                    BarHeight = hours <= 0 ? 3 : Math.Max(6, hours / maxHours * ChartMaxHeight),
                    IsToday = day == today
                });
            }
        }

        private void LoadHistory()
        {
            HistoryRows.Clear();

            var sessions = _db.FocusSessions
                .Where(f => !f.IsHidden)
                .OrderByDescending(f => f.StartedAt)
                .Take(200)
                .ToList();

            foreach (var s in sessions)
            {
                HistoryRows.Add(new FocusSessionRowViewModel
                {
                    DateLabel = s.StartedAt.ToString("ddd, MMM d", CultureInfo.InvariantCulture),
                    TimeRangeLabel = $"{s.StartedAt:HH:mm} - {s.CompletedAt:HH:mm}",
                    DurationLabel = $"{s.DurationMinutes} min"
                });
            }
        }

        private void ClearHistory()
        {
            var result = MessageBox.Show(
                "This will clear your session history list. Your totals and streak will be kept. Continue?",
                "Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Soft delete: hide from History, but keep the rows so Summary
            // (hours, streak, days accessed) stays accurate.
            var visibleSessions = _db.FocusSessions.Where(f => !f.IsHidden);
            foreach (var s in visibleSessions)
            {
                s.IsHidden = true;
            }
            _db.SaveChanges();

            _weekOffset = 0;
            RefreshAll();
            NextWeekCommand.RaiseCanExecuteChanged();
        }
    }
}