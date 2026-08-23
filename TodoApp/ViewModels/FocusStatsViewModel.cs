using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TodoApp.Repositories;

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

    public class FocusStatsViewModel : ViewModelBase, IDisposable
    {
        private readonly IFocusSessionRepository _sessionRepo;
        private const double ChartMaxHeight = 130;
        private bool _disposed;

        public FocusStatsViewModel(IFocusSessionRepository sessionRepo)
        {
            _sessionRepo = sessionRepo;

            PrevWeekCommand = new RelayCommand(async _ => await ChangeWeekAsync(-1));
            NextWeekCommand = new RelayCommand(async _ => await ChangeWeekAsync(1), _ => _weekOffset < 0);
            ClearHistoryCommand = new RelayCommand(async _ => await ClearHistoryAsync());
            ShowSummaryTabCommand = new RelayCommand(_ => IsHistoryTabActive = false);
            ShowHistoryTabCommand = new RelayCommand(_ => IsHistoryTabActive = true);

            _ = RefreshAllAsync();
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

        private async Task ChangeWeekAsync(int delta)
        {
            try
            {
                var newOffset = _weekOffset + delta;
                if (newOffset > 0) return;
                _weekOffset = newOffset;
                await LoadWeekBarsAsync();
                NextWeekCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load week data:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task RefreshAllAsync()
        {
            try
            {
                await LoadSummaryAsync();
                await LoadWeekBarsAsync();
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load statistics:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadSummaryAsync()
        {
            HoursFocusedToday = await _sessionRepo.GetTodayHoursAsync();
            DaysAccessed = await _sessionRepo.GetTotalDaysAsync();
            DayStreak = await _sessionRepo.GetStreakAsync();

            var totalDays = await _sessionRepo.GetTotalDaysAsync();
            HasHistory = totalDays > 0;
        }

        private async Task LoadWeekBarsAsync()
        {
            WeekBars.Clear();

            var today = DateTime.Today;
            var mondayThisWeek = today.AddDays(-(int)today.DayOfWeek + (today.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
            var weekStart = mondayThisWeek.AddDays(_weekOffset * 7);
            var weekEnd = weekStart.AddDays(7);

            WeekRangeLabel = _weekOffset == 0
                ? "This Week"
                : $"{weekStart:MMM d} - {weekEnd.AddDays(-1):MMM d}";

            var dailyHours = await _sessionRepo.GetWeeklyHoursAsync(weekStart);
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

        private async Task LoadHistoryAsync()
        {
            HistoryRows.Clear();
            var sessions = await _sessionRepo.GetVisibleAsync();

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

        private async Task ClearHistoryAsync()
        {
            var result = MessageBox.Show(
                "This will clear your session history list. Your totals and streak will be kept. Continue?",
                "Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _sessionRepo.SoftDeleteAllVisibleAsync();
                await _sessionRepo.SaveChangesAsync();

                _weekOffset = 0;
                await RefreshAllAsync();
                NextWeekCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to clear history:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
