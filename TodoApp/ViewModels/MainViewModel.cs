using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.Views;

namespace TodoApp.ViewModels
{
    public enum TaskFilter
    {
        All,
        Active,
        Completed,
        Starred,
        Archived
    }

    public enum TaskSortMode
    {
        Manual,
        DueDate,
        Priority,
        CreatedAt
    }

    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly ITodoRepository _todoRepo;
        private bool _disposed;

        public ObservableCollection<TodoItemViewModel> AllTodos { get; } = new();
        public ICollectionView TodosView { get; }
        public ObservableCollection<string> Categories { get; } = new() { "All Categories" };

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                    TodosView.Refresh();
            }
        }

        private TaskFilter _filter = TaskFilter.All;
        public TaskFilter Filter
        {
            get => _filter;
            set
            {
                if (SetField(ref _filter, value))
                    TodosView.Refresh();
            }
        }

        private string _selectedCategory = "All Categories";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetField(ref _selectedCategory, value))
                    TodosView.Refresh();
            }
        }

        private int _sortModeIndex;
        public int SortModeIndex
        {
            get => _sortModeIndex;
            set
            {
                if (SetField(ref _sortModeIndex, value))
                    ApplySorting();
            }
        }

        public TaskSortMode SortMode => (TaskSortMode)_sortModeIndex;

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        private double _completionPercentage;
        public double CompletionPercentage
        {
            get => _completionPercentage;
            set => SetField(ref _completionPercentage, value);
        }

        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            set => SetField(ref _completedCount, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetField(ref _totalCount, value);
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ClearCompletedCommand { get; }
        public RelayCommand SetFilterCommand { get; }
        public RelayCommand OpenTimerCommand { get; }
        public RelayCommand OpenCategoryDialogCommand { get; }
        public RelayCommand AddSubTaskCommand { get; }
        public RelayCommand DeleteSubTaskCommand { get; }
        public RelayCommand UndoDeleteCommand { get; }
        public RelayCommand ToggleFavoriteCommand { get; }
        public RelayCommand ArchiveTaskCommand { get; }
        public RelayCommand UnarchiveTaskCommand { get; }
        public RelayCommand OpenDetailCommand { get; }
        public RelayCommand OpenDashboardCommand { get; }
        public RelayCommand OpenThemePickerCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand ClearSelectionCommand { get; }

        private PomodoroWindow? _timerWindow;
        private DashboardWindow? _dashboardWindow;
        private List<TodoItem>? _lastDeletedItems;

        public bool HasSelection => AllTodos.Any(t => t.IsSelected);

        public IReadOnlyList<TodoItemViewModel> SelectedTodos =>
            AllTodos.Where(t => t.IsSelected).ToList();

        public MainViewModel(ITodoRepository todoRepo)
        {
            _todoRepo = todoRepo;

            TodosView = CollectionViewSource.GetDefaultView(AllTodos);
            TodosView.Filter = FilterPredicate;
            ApplySorting();

            AddCommand = new RelayCommand(_ => AddTodo());
            EditCommand = new RelayCommand(
                p => EditTodo(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
            DeleteCommand = new RelayCommand(
                p => DeleteTodo(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
            ClearCompletedCommand = new RelayCommand(
                _ => ClearCompleted(),
                _ => AllTodos.Any(t => t.IsCompleted));
            SetFilterCommand = new RelayCommand(p =>
            {
                if (p is string value && Enum.TryParse<TaskFilter>(value, out var filter))
                    Filter = filter;
            });
            OpenTimerCommand = new RelayCommand(_ => OpenTimer());
            OpenCategoryDialogCommand = new RelayCommand(_ => OpenCategoryDialog());
            AddSubTaskCommand = new RelayCommand(
                p => AddSubTask(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
            DeleteSubTaskCommand = new RelayCommand(p => DeleteSubTask(p as TodoItemViewModel));
            UndoDeleteCommand = new RelayCommand(_ => UndoDelete(), _ => _lastDeletedItems != null);
            ToggleFavoriteCommand = new RelayCommand(p => ToggleFavorite(p as TodoItemViewModel));
            ArchiveTaskCommand = new RelayCommand(
                p => ArchiveTask(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
            UnarchiveTaskCommand = new RelayCommand(
                p => UnarchiveTask(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
            OpenDetailCommand = new RelayCommand(
                p => OpenDetail(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
            OpenDashboardCommand = new RelayCommand(_ => OpenDashboard());
            OpenThemePickerCommand = new RelayCommand(_ => OpenThemePicker());
            DeleteSelectedCommand = new RelayCommand(
                _ => DeleteSelected(),
                _ => HasSelection);
            ClearSelectionCommand = new RelayCommand(_ => ClearSelection());

            _ = LoadFromDatabaseAsync();
        }

        private void OnItemSelectionChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(HasSelection));
            DeleteSelectedCommand.RaiseCanExecuteChanged();
        }

        public async System.Threading.Tasks.Task LoadFromDatabaseAsync()
        {
            try
            {
                AllTodos.Clear();
                var todos = await _todoRepo.GetAllAsync();

                var byId = new Dictionary<int, TodoItemViewModel>();
                foreach (var item in todos)
                    byId[item.Id] = new TodoItemViewModel(item);

                foreach (var item in todos)
                {
                    var vm = byId[item.Id];
                    if (item.ParentId.HasValue && byId.TryGetValue(item.ParentId.Value, out var parent))
                        parent.SubTasks.Add(vm);
                    else
                        AddToCollection(vm);
                }

                foreach (var root in AllTodos)
                    root.RefreshSubTasks();

                await RefreshCategoriesAsync();
                UpdateStatus();
                CheckReminders(showInfo: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load tasks:\n\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void CheckReminders(bool showInfo = true)
        {
            try
            {
                var overdue = AllTodos
                    .Where(t => t.IsOverdue && !t.IsArchived)
                    .ToList();
                var dueToday = AllTodos
                    .Where(t => !t.IsCompleted && !t.IsArchived && t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today)
                    .ToList();

                if (!showInfo) return;

                if (overdue.Count == 0 && dueToday.Count == 0) return;

                var lines = new List<string>();
                if (overdue.Count > 0)
                    lines.Add($"{overdue.Count} task(s) are OVERDUE");
                if (dueToday.Count > 0)
                    lines.Add($"{dueToday.Count} task(s) are due TODAY");

                var details = string.Join("\n",
                    overdue.Select(t => $"⚠ {t.Title}")
                        .Concat(dueToday.Select(t => $"• {t.Title}"))
                        .Take(8));

                MessageBox.Show(
                    $"{string.Join("\n", lines)}\n\n{details}",
                    "Task Reminders",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                // reminders are best-effort; never crash on them
            }
        }

        private void AddToCollection(TodoItemViewModel vm)
        {
            vm.IsCompletedChanged += OnItemCompletionChanged;
            vm.SelectionChanged += OnItemSelectionChanged;
            AllTodos.Add(vm);
        }

        public async System.Threading.Tasks.Task ReorderTodoAsync(TodoItemViewModel dragged, TodoItemViewModel target)
        {
            if (dragged == target) return;
            if (SortMode != TaskSortMode.Manual) return;

            var oldIndex = AllTodos.IndexOf(dragged);
            var newIndex = AllTodos.IndexOf(target);
            if (oldIndex < 0 || newIndex < 0) return;

            AllTodos.Move(oldIndex, newIndex);

            for (int i = 0; i < AllTodos.Count; i++)
                AllTodos[i].SortOrder = i;

            try
            {
                await _todoRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save order:\n\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            TodosView.Refresh();
        }

        private async void OnItemCompletionChanged(object? sender, EventArgs e)
        {
            if (sender is not TodoItemViewModel vm) return;

            try
            {
                if (vm.IsCompleted)
                {
                    foreach (var sub in vm.SubTasks.Where(s => !s.IsCompleted))
                        sub.MarkCompletedQuietly();
                    vm.RefreshSubTasks();
                }

                if (vm.IsCompleted && vm.Recurrence != RecurrenceType.None)
                    await CreateNextOccurrenceAsync(vm);

                await _todoRepo.SaveChangesAsync();
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save:\n\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task CreateNextOccurrenceAsync(TodoItemViewModel vm)
        {
            var baseDate = vm.DueDate?.Date ?? DateTime.Today;
            var nextDate = vm.Recurrence switch
            {
                RecurrenceType.Daily => baseDate.AddDays(1),
                RecurrenceType.Weekly => baseDate.AddDays(7),
                RecurrenceType.Monthly => baseDate.AddMonths(1),
                _ => baseDate
            };

            var next = new TodoItem
            {
                Title = vm.Title,
                Description = vm.Description,
                Category = vm.Category,
                Priority = vm.Priority,
                DueDate = nextDate,
                CreatedAt = DateTime.Now,
                SortOrder = vm.SortOrder,
                Recurrence = vm.Recurrence
            };

            await _todoRepo.AddAsync(next);
            await _todoRepo.SaveChangesAsync();
            AddToCollection(new TodoItemViewModel(next));
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not TodoItemViewModel vm) return false;

            if (Filter == TaskFilter.Archived)
                return vm.IsArchived && MatchesSearchAndCategory(vm);

            if (vm.IsArchived) return false;

            switch (Filter)
            {
                case TaskFilter.Starred:
                    if (!vm.IsFavorite) return false;
                    break;
                case TaskFilter.Active:
                    if (vm.IsCompleted) return false;
                    break;
                case TaskFilter.Completed:
                    if (!vm.IsCompleted) return false;
                    break;
            }

            return MatchesSearchAndCategory(vm);
        }

        private bool MatchesSearchAndCategory(TodoItemViewModel vm)
        {
            if (SelectedCategory != "All Categories")
            {
                var category = string.IsNullOrWhiteSpace(vm.Category) ? "Uncategorized" : vm.Category;
                if (category != SelectedCategory) return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                var inTitle = vm.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
                var inDescription = vm.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
                var inTags = vm.Tags?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
                var inSubTasks = vm.SubTasks.Any(s =>
                    (s.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
                if (!inTitle && !inDescription && !inTags && !inSubTasks) return false;
            }

            return true;
        }

        private void ApplySorting()
        {
            if (TodosView == null) return;

            TodosView.SortDescriptions.Clear();
            switch (SortMode)
            {
                case TaskSortMode.DueDate:
                    TodosView.SortDescriptions.Add(
                        new SortDescription(nameof(TodoItemViewModel.IsCompleted), ListSortDirection.Ascending));
                    TodosView.SortDescriptions.Add(
                        new SortDescription(nameof(TodoItemViewModel.DueDate), ListSortDirection.Ascending));
                    break;
                case TaskSortMode.Priority:
                    TodosView.SortDescriptions.Add(
                        new SortDescription(nameof(TodoItemViewModel.IsCompleted), ListSortDirection.Ascending));
                    TodosView.SortDescriptions.Add(
                        new SortDescription(nameof(TodoItemViewModel.Priority), ListSortDirection.Descending));
                    break;
                case TaskSortMode.CreatedAt:
                    TodosView.SortDescriptions.Add(
                        new SortDescription(nameof(TodoItemViewModel.CreatedAt), ListSortDirection.Descending));
                    break;
                case TaskSortMode.Manual:
                default:
                    TodosView.SortDescriptions.Add(
                        new SortDescription(nameof(TodoItemViewModel.SortOrder), ListSortDirection.Ascending));
                    break;
            }
            TodosView.Refresh();
        }

        private void OpenTimer()
        {
            if (_timerWindow != null)
            {
                _timerWindow.Activate();
                if (_timerWindow.WindowState == WindowState.Minimized)
                    _timerWindow.WindowState = WindowState.Normal;
                return;
            }

            _timerWindow = new PomodoroWindow();
            if (Application.Current.MainWindow != null)
                _timerWindow.Owner = Application.Current.MainWindow;

            _timerWindow.Closed += (_, _) => _timerWindow = null;
            _timerWindow.Show();
        }

        private async void AddTodo()
        {
            try
            {
                var dialog = new AddEditTodoWindow();
                if (Application.Current.MainWindow != null)
                    dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() != true || dialog.ResultItem == null) return;

                var minOrder = AllTodos.Count > 0 ? AllTodos.Min(t => t.SortOrder) : 0;
                dialog.ResultItem.SortOrder = minOrder - 1;

                await _todoRepo.AddAsync(dialog.ResultItem);
                await _todoRepo.SaveChangesAsync();

                AddToCollection(new TodoItemViewModel(dialog.ResultItem));
                TodosView.Refresh();
                await RefreshCategoriesAsync();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to add task:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void AddSubTask(TodoItemViewModel? parent)
        {
            if (parent == null) return;

            try
            {
                var dialog = new AddEditTodoWindow(isSubTask: true);
                if (Application.Current.MainWindow != null)
                    dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() != true || dialog.ResultItem == null) return;

                dialog.ResultItem.ParentId = parent.Model.Id;
                dialog.ResultItem.SortOrder = parent.SubTasks.Count;

                await _todoRepo.AddAsync(dialog.ResultItem);
                await _todoRepo.SaveChangesAsync();

                parent.AddSubTask(new TodoItemViewModel(dialog.ResultItem));
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to add sub-task:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeleteSubTask(TodoItemViewModel? subVm)
        {
            if (subVm?.Model.ParentId == null) return;

            var parent = AllTodos.FirstOrDefault(t => t.Model.Id == subVm.Model.ParentId);
            if (parent == null) return;

            var confirm = MessageBox.Show(
                $"Delete \"{subVm.Title}\"?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _todoRepo.DeleteAsync(subVm.Model);
                await _todoRepo.SaveChangesAsync();

                parent.SubTasks.Remove(subVm);
                parent.RefreshSubTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete sub-task:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void EditTodo(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            try
            {
                var dialog = new AddEditTodoWindow(vm.Model);
                if (Application.Current.MainWindow != null)
                    dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() != true || dialog.ResultItem == null) return;

                vm.Title = dialog.ResultItem.Title;
                vm.Description = dialog.ResultItem.Description;
                vm.Category = dialog.ResultItem.Category;
                vm.Priority = dialog.ResultItem.Priority;
                vm.DueDate = dialog.ResultItem.DueDate;
                vm.Recurrence = dialog.ResultItem.Recurrence;

                await _todoRepo.SaveChangesAsync();
                await RefreshCategoriesAsync();
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to edit task:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeleteTodo(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            var result = MessageBox.Show(
                $"Delete \"{vm.Title}\"?" + (vm.HasSubTasks ? $"\n\nIts {vm.SubTasks.Count} sub-task(s) will also be deleted." : ""),
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var snapshot = new List<TodoItem> { CloneItem(vm.Model) };
                snapshot.AddRange(vm.SubTasks.Select(s => CloneItem(s.Model)));

                vm.IsCompletedChanged -= OnItemCompletionChanged;

                if (vm.HasSubTasks)
                    foreach (var sub in vm.SubTasks.ToList())
                        await _todoRepo.DeleteAsync(sub.Model);

                await _todoRepo.DeleteAsync(vm.Model);
                await _todoRepo.SaveChangesAsync();

                _lastDeletedItems = snapshot;
                UndoDeleteCommand.RaiseCanExecuteChanged();

                AllTodos.Remove(vm);
                await RefreshCategoriesAsync();
                UpdateStatus();

                StatusText += "  ·  Ctrl+Z to undo";
            }
            catch (Exception ex)
            {
                vm.IsCompletedChanged += OnItemCompletionChanged;
                vm.SelectionChanged += OnItemSelectionChanged;
                MessageBox.Show(
                    $"Failed to delete task:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static TodoItem CloneItem(TodoItem source) => new()
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            Category = source.Category,
            Priority = source.Priority,
            DueDate = source.DueDate,
            IsCompleted = source.IsCompleted,
            CreatedAt = source.CreatedAt,
            CompletedAt = source.CompletedAt,
            SortOrder = source.SortOrder,
            ParentId = source.ParentId,
            Recurrence = source.Recurrence,
            Tags = source.Tags,
            Icon = source.Icon,
            IsFavorite = source.IsFavorite,
            IsArchived = source.IsArchived,
            Attachments = source.Attachments
        };

        private async void UndoDelete()
        {
            if (_lastDeletedItems == null || _lastDeletedItems.Count == 0) return;

            try
            {
                var idMap = new Dictionary<int, int>();
                var pending = new List<TodoItem>(_lastDeletedItems);
                var guard = 0;

                while (pending.Count > 0 && guard++ < 100)
                {
                    var batch = pending
                        .Where(i => i.ParentId == null || idMap.ContainsKey(i.ParentId.Value))
                        .ToList();

                    if (batch.Count == 0)
                    {
                        foreach (var orphan in pending)
                        {
                            orphan.Id = 0;
                            orphan.ParentId = null;
                            await _todoRepo.AddAsync(orphan);
                        }
                        break;
                    }

                    foreach (var item in batch)
                    {
                        var originalId = item.Id;
                        item.Id = 0;

                        if (item.ParentId.HasValue && idMap.TryGetValue(item.ParentId.Value, out var mapped))
                            item.ParentId = mapped;

                        await _todoRepo.AddAsync(item);
                        await _todoRepo.SaveChangesAsync();

                        if (originalId != 0)
                            idMap[originalId] = item.Id;

                        pending.Remove(item);
                    }
                }

                await _todoRepo.SaveChangesAsync();

                _lastDeletedItems = null;
                UndoDeleteCommand.RaiseCanExecuteChanged();

                await LoadFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to restore:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ClearCompleted()
        {
            var completed = AllTodos
                .Where(t => t.IsCompleted && !t.IsArchived)
                .ToList();
            if (!completed.Any()) return;

            var confirmResult = MessageBox.Show(
                $"Remove {completed.Count} completed task(s)?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                _lastDeletedItems = completed
                    .SelectMany(t => new[] { t }.Concat(t.SubTasks))
                    .Select(t => CloneItem(t.Model))
                    .ToList();
                UndoDeleteCommand.RaiseCanExecuteChanged();

                foreach (var item in completed)
                {
                    item.IsCompletedChanged -= OnItemCompletionChanged;
                    AllTodos.Remove(item);
                }

                await _todoRepo.DeleteRangeAsync(completed.SelectMany(t => new[] { t.Model }.Concat(t.SubTasks.Select(s => s.Model))));
                await _todoRepo.SaveChangesAsync();
                await RefreshCategoriesAsync();
                UpdateStatus();

                StatusText += "  ·  Ctrl+Z to undo";
            }
            catch (Exception ex)
            {
                foreach (var item in completed)
                    item.IsCompletedChanged += OnItemCompletionChanged;

                MessageBox.Show(
                    $"Failed to clear completed tasks:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ToggleFavorite(TodoItemViewModel? vm)
        {
            if (vm == null) return;
            vm.IsFavorite = !vm.IsFavorite;

            try
            {
                await _todoRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save favorite:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ArchiveTask(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            try
            {
                vm.Model.IsArchived = true;
                await _todoRepo.SaveChangesAsync();
                await LoadFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to archive:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UnarchiveTask(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            try
            {
                var repoItem = await _todoRepo.GetByIdAsync(vm.Id);
                if (repoItem == null) return;
                repoItem.IsArchived = false;
                await _todoRepo.SaveChangesAsync();
                await LoadFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unarchive:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void OpenDetail(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            try
            {
                var dialog = new TaskDetailWindow(vm.Model, Categories.ToList());
                if (Application.Current.MainWindow != null)
                    dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() != true || dialog.ResultItem == null) return;

                var updated = dialog.ResultItem;
                vm.Title = updated.Title;
                vm.Description = updated.Description;
                vm.Category = updated.Category;
                vm.Priority = updated.Priority;
                vm.DueDate = updated.DueDate;
                vm.Recurrence = updated.Recurrence;
                vm.Icon = updated.Icon;
                vm.Tags = updated.Tags;
                vm.Model.Attachments = updated.Attachments;

                await _todoRepo.SaveChangesAsync();
                await RefreshCategoriesAsync();
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save task details:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDashboard()
        {
            if (_dashboardWindow != null)
            {
                _dashboardWindow.Activate();
                return;
            }

            _dashboardWindow = new DashboardWindow(AllTodos.ToList());
            if (Application.Current.MainWindow != null)
                _dashboardWindow.Owner = Application.Current.MainWindow;
            _dashboardWindow.Closed += (_, _) => _dashboardWindow = null;
            _dashboardWindow.Show();
        }

        private void OpenThemePicker()
        {
            var dialog = new ThemePickerWindow();
            if (Application.Current.MainWindow != null)
                dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();

            TodosView.Refresh();
        }

        private void ClearSelection()
        {
            foreach (var t in AllTodos.Where(t => t.IsSelected))
                t.IsSelected = false;
        }

        private async void DeleteSelected()
        {
            var selected = SelectedTodos;
            if (selected.Count == 0) return;

            var result = MessageBox.Show(
                $"Delete {selected.Count} selected task(s)?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var snapshot = new List<TodoItem>();
                foreach (var vm in selected)
                {
                    snapshot.Add(CloneItem(vm.Model));
                    snapshot.AddRange(vm.SubTasks.Select(s => CloneItem(s.Model)));

                    vm.IsCompletedChanged -= OnItemCompletionChanged;
                    vm.SelectionChanged -= OnItemSelectionChanged;

                    if (vm.HasSubTasks)
                        foreach (var sub in vm.SubTasks.ToList())
                            await _todoRepo.DeleteAsync(sub.Model);

                    await _todoRepo.DeleteAsync(vm.Model);
                    AllTodos.Remove(vm);
                }

                await _todoRepo.SaveChangesAsync();

                _lastDeletedItems = snapshot;
                UndoDeleteCommand.RaiseCanExecuteChanged();

                OnPropertyChanged(nameof(HasSelection));
                DeleteSelectedCommand.RaiseCanExecuteChanged();
                await RefreshCategoriesAsync();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                await LoadFromDatabaseAsync();
                MessageBox.Show($"Failed to delete selection:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task RefreshCategoriesAsync()
        {
            var current = SelectedCategory;
            Categories.Clear();
            Categories.Add("All Categories");

            var categories = AllTodos
                .SelectMany(t => new[] { t.Category }.Concat(t.SubTasks.Select(s => s.Category)))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct()
                .OrderBy(c => c);

            foreach (var category in categories)
                Categories.Add(category);

            SelectedCategory = Categories.Contains(current) ? current : "All Categories";
        }

        public async void OpenCategoryDialog()
        {
            var dialog = new AddCategoryWindow(Categories);
            if (Application.Current.MainWindow != null)
                dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() != true) return;

            if (dialog.IsDelete)
            {
                await DeleteCategoryAsync(dialog.CategoryToDelete);
                return;
            }

            var newCategory = dialog.CategoryName;
            if (string.IsNullOrWhiteSpace(newCategory)) return;

            if (!Categories.Contains(newCategory))
                Categories.Add(newCategory);

            SelectedCategory = newCategory;
            TodosView.Refresh();
        }

        private async System.Threading.Tasks.Task DeleteCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            if (category == "All Categories" || category == "Uncategorized") return;

            var todosWithCategory = AllTodos
                .SelectMany(t => new[] { t }.Concat(t.SubTasks))
                .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!todosWithCategory.Any())
            {
                await RefreshCategoriesAsync();
                return;
            }

            foreach (var todo in todosWithCategory)
                todo.Category = null;

            await _todoRepo.SaveChangesAsync();
            await RefreshCategoriesAsync();
            TodosView.Refresh();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var visible = AllTodos.Where(t => !t.IsArchived).ToList();
            var total = visible.Count;
            var active = visible.Count(t => !t.IsCompleted);
            var completed = total - active;

            StatusText = $"{active} active / {total} total";
            TotalCount = total;
            CompletedCount = completed;
            CompletionPercentage = total == 0 ? 0 : Math.Round(completed * 100.0 / total, 1);

            ClearCompletedCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var todo in AllTodos)
            {
                todo.IsCompletedChanged -= OnItemCompletionChanged;
                todo.SelectionChanged -= OnItemSelectionChanged;
            }

            AllTodos.Clear();
        }
    }
}
