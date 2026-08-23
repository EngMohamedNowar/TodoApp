using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using TodoApp.Models;
using TodoApp.Repositories;

namespace TodoApp.ViewModels
{
    public class StatCardViewModel : ViewModelBase
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Accent { get; set; } = "#8B7CF6";
    }

    public class DayCompletionViewModel : ViewModelBase
    {
        public string DayLabel { get; set; } = "";
        public int Count { get; set; }
        public double BarHeight { get; set; }
        public bool IsToday { get; set; }
    }

    public class CategoryStatViewModel : ViewModelBase
    {
        public string Name { get; set; } = "Uncategorized";
        public int Total { get; set; }
        public int Completed { get; set; }
        public double Percent => Total == 0 ? 0 : Completed * 100.0 / Total;
        public string PercentText => $"{Math.Round(Percent)}%";
    }

    public class DashboardViewModel : ViewModelBase
    {
        private const double ChartMaxHeight = 120;
        private readonly List<TodoItemViewModel> _todos;

        public ObservableCollection<StatCardViewModel> StatCards { get; } = new();
        public ObservableCollection<DayCompletionViewModel> WeekBars { get; } = new();
        public ObservableCollection<CategoryStatViewModel> CategoryStats { get; } = new();

        private int _focusSessionsToday;
        public int FocusSessionsToday
        {
            get => _focusSessionsToday;
            private set => SetField(ref _focusSessionsToday, value);
        }

        private double _focusHoursToday;
        public double FocusHoursToday
        {
            get => _focusHoursToday;
            private set => SetField(ref _focusHoursToday, value);
        }

        public string FocusSummary => $"{FocusHoursToday:0.#}h focused · {FocusSessionsToday} session(s) today";

        public DashboardViewModel(List<TodoItemViewModel> todos, IFocusSessionRepository? sessionRepo = null)
        {
            _todos = todos ?? new List<TodoItemViewModel>();

            if (sessionRepo != null)
                _ = LoadFocusStatsAsync(sessionRepo);

            BuildTaskCards();
            BuildWeeklyChart();
            BuildCategoryBreakdown();
        }

        private async System.Threading.Tasks.Task LoadFocusStatsAsync(IFocusSessionRepository sessionRepo)
        {
            try
            {
                FocusSessionsToday = await sessionRepo.GetTodayCountAsync();
                FocusHoursToday = await sessionRepo.GetTodayHoursAsync();
                OnPropertyChanged(nameof(FocusSummary));
            }
            catch
            {
                // stats are best-effort
            }
        }

        private IEnumerable<TodoItemViewModel> AllFlat() =>
            _todos
                .Where(t => !t.IsArchived)
                .SelectMany(t => new[] { t }.Concat(t.SubTasks));

        private void BuildTaskCards()
        {
            var all = AllFlat().ToList();

            var total = all.Count;
            var completed = all.Count(t => t.IsCompleted);
            var overdue = all.Count(t => t.IsOverdue);
            var starred = all.Count(t => t.IsFavorite);

            StatCards.Add(new StatCardViewModel { Label = "Total tasks", Value = total.ToString(), Accent = "#8B7CF6" });
            StatCards.Add(new StatCardViewModel
            {
                Label = "Completed",
                Value = total == 0 ? "0%" : $"{Math.Round(completed * 100.0 / total)}%",
                Accent = "#51CF66"
            });
            StatCards.Add(new StatCardViewModel { Label = "Overdue", Value = overdue.ToString(), Accent = "#FF6B6B" });
            StatCards.Add(new StatCardViewModel { Label = "Starred", Value = starred.ToString(), Accent = "#FFA94D" });
        }

        private void BuildWeeklyChart()
        {
            var completedDates = AllFlat()
                .Where(t => t.IsCompleted && t.Model.CompletedAt.HasValue)
                .Select(t => t.Model.CompletedAt!.Value.Date)
                .ToHashSet();

            var today = DateTime.Today;
            var maxCount = 1.0;

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                maxCount = Math.Max(maxCount, 1);
            }

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var count = completedDates.Count(d => d == day);
                maxCount = Math.Max(maxCount, count);
                WeekBars.Add(new DayCompletionViewModel
                {
                    DayLabel = day.ToString("ddd", CultureInfo.InvariantCulture),
                    Count = count,
                    IsToday = i == 0,
                    BarHeight = count == 0 ? 3 : Math.Max(8, count / maxCount * ChartMaxHeight)
                });
            }
        }

        private void BuildCategoryBreakdown()
        {
            var groups = AllFlat()
                .Where(t => !t.IsArchived)
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Uncategorized" : t.Category!)
                .OrderByDescending(g => g.Count());

            foreach (var group in groups)
            {
                CategoryStats.Add(new CategoryStatViewModel
                {
                    Name = group.Key,
                    Total = group.Count(),
                    Completed = group.Count(t => t.IsCompleted)
                });
            }
        }
    }
}
