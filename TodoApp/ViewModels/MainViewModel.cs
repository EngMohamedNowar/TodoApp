using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.Services;

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

    public enum SubTaskSortMode
    {
        Manual,
        Priority
    }

    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly ITodoRepository _todoRepo;
        private readonly IDialogService _dialogs;
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

        private int _subSortModeIndex;
        public int SubTaskSortModeIndex
        {
            get => _subSortModeIndex;
            set
            {
                if (SetField(ref _subSortModeIndex, value))
                    ApplySubTaskSorting();
            }
        }

        public SubTaskSortMode SubSortMode => (SubTaskSortMode)_subSortModeIndex;

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

        private List<TodoItem>? _lastDeletedItems;

        public bool HasSelection => AllTodos.Any(t => t.IsSelected);

        public IReadOnlyList<TodoItemViewModel> SelectedTodos =>
            AllTodos.Where(t => t.IsSelected).ToList();

        public MainViewModel(ITodoRepository todoRepo, IDialogService dialogs)
        {
            _todoRepo = todoRepo;
            _dialogs = dialogs;

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
                _ => AllTodos.Any(t => !t.IsArchived &&
                                       (t.IsCompleted || t.SubTasks.Any(s => s.IsCompleted))));
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
            DeleteSubTaskCommand = new RelayCommand(
                p => DeleteSubTask(p as TodoItemViewModel),
                p => p is TodoItemViewModel);
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
                        parent.AddSubTask(vm);
                    else
                        AddToCollection(vm);
                }

                foreach (var root in AllTodos)
                {
                    AttachEvents(root);
                    root.RefreshSubTasks();
                }

                await RefreshCategoriesAsync();
                ApplySubTaskSorting();
                UpdateStatus();
                CheckReminders(showInfo: false);
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(
                    $"Failed to load tasks:\n\n{ex.Message}",
                    "Database Error");
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

                _dialogs.ShowInfo(
                    $"{string.Join("\n", lines)}\n\n{details}",
                    "Task Reminders");
            }
            catch (Exception ex)
            {
                // reminders are best-effort; never crash on them
                System.Diagnostics.Debug.WriteLine($"CheckReminders failed: {ex}");
            }
        }

        private void AddToCollection(TodoItemViewModel vm)
        {
            AllTodos.Add(vm);
            AttachEvents(vm);
        }

        /// <summary>
        /// Subscribes a card (and every nested sub-task) to the handlers that persist
        /// changes. Sub-tasks are added straight onto their parent's collection when the
        /// tree is rebuilt, so they must be wired up explicitly or ticking their
        /// checkbox would never reach the database.
        /// </summary>
        private void AttachEvents(TodoItemViewModel vm)
        {
            vm.IsCompletedChanged -= OnItemCompletionChanged;
            vm.IsCompletedChanged += OnItemCompletionChanged;
            vm.SelectionChanged -= OnItemSelectionChanged;
            vm.SelectionChanged += OnItemSelectionChanged;

            foreach (var sub in vm.SubTasks)
                AttachEvents(sub);
        }

        private void DetachEvents(TodoItemViewModel vm)
        {
            vm.IsCompletedChanged -= OnItemCompletionChanged;
            vm.SelectionChanged -= OnItemSelectionChanged;

            foreach (var sub in vm.SubTasks)
                DetachEvents(sub);
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
                _dialogs.ShowError($"Failed to save order:\n\n{ex.Message}", "Database Error");
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
                }

                vm.RefreshSubTasks();
                vm.ParentVm?.RefreshSubTasks();

                if (vm.IsCompleted && vm.Recurrence != RecurrenceType.None)
                    await CreateNextOccurrenceAsync(vm);

                await _todoRepo.SaveChangesAsync();
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to save:\n\n{ex.Message}", "Database Error");
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
                var inSubTasks = vm.SelfAndDescendants().Skip(1).Any(s =>
                    (s.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Tags?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
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

        private void ApplySubTaskSorting()
        {
            foreach (var root in AllTodos)
                SortSubTasksRecursive(root);
        }

        private void SortSubTasksRecursive(TodoItemViewModel todo)
        {
            var subs = todo.SubTasks;

            if (subs.Count >= 2)
            {
                var ordered = SubSortMode == SubTaskSortMode.Priority
                    ? subs.OrderByDescending(s => s.Priority).ThenBy(s => s.SortOrder).ToList()
                    : subs.OrderBy(s => s.SortOrder).ToList();

                for (var i = 0; i < ordered.Count; i++)
                {
                    var at = subs.IndexOf(ordered[i]);
                    if (at != i)
                        subs.Move(at, i);
                }
            }

            foreach (var sub in todo.SubTasks.ToList())
                SortSubTasksRecursive(sub);
        }

        private void OpenTimer() => _dialogs.ShowTimer();

        private async System.Threading.Tasks.Task AddTodo()
        {
            try
            {
                var item = _dialogs.NewTask(Categories.ToList());
                if (item == null) return;

                var minOrder = AllTodos.Count > 0 ? AllTodos.Min(t => t.SortOrder) : 0;
                item.SortOrder = minOrder - 1;

                await _todoRepo.AddAsync(item);
                await _todoRepo.SaveChangesAsync();

                AddToCollection(new TodoItemViewModel(item));
                TodosView.Refresh();
                await RefreshCategoriesAsync();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to add task:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task AddSubTask(TodoItemViewModel? parent)
        {
            if (parent == null) return;

            try
            {
                if (parent.Model.Id == 0)
                {
                    _dialogs.ShowError(
                        "The parent task has not been saved yet, so its sub-task cannot be linked to it.",
                        "Cannot Add Sub-Task");
                    return;
                }

                var dialog = _dialogs.NewSubTask(Categories.ToList());
                if (dialog == null) return;

                dialog.ParentId = parent.Model.Id;
                dialog.SortOrder = parent.SubTasks.Count;

                await _todoRepo.AddAsync(dialog);
                await _todoRepo.SaveChangesAsync();

                var subVm = new TodoItemViewModel(dialog);
                parent.AddSubTask(subVm);
                AttachEvents(subVm);
                SortSubTasksRecursive(parent);

                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to add sub-task:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task DeleteSubTask(TodoItemViewModel? subVm)
        {
            if (subVm?.Model.ParentId == null) return;

            var parent = AllTodos.FirstOrDefault(t => t.Model.Id == subVm.Model.ParentId);
            if (parent == null) return;

            if (!_dialogs.Confirm($"Delete \"{subVm.Title}\"?", "Confirm Delete")) return;

            try
            {
                var subtree = new List<TodoItemViewModel>();
                CollectSubtree(subVm, subtree);

                // deepest-first so optional FKs are released before their owners go
                for (int i = subtree.Count - 1; i >= 0; i--)
                    await _todoRepo.DeleteAsync(subtree[i].Model);

                await _todoRepo.SaveChangesAsync();

                DetachEvents(subVm);
                parent.SubTasks.Remove(subVm);
                parent.RefreshSubTasks();
                subVm.ParentVm = null;

                _lastDeletedItems = subtree.Select(s => CloneItem(s.Model)).ToList();
                UndoDeleteCommand.RaiseCanExecuteChanged();

                TodosView.Refresh();
                UpdateStatus();
                StatusText += "  ·  Ctrl+Z to undo";
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to delete sub-task:\n\n{ex.Message}", "Error");
            }
        }

        private static void CollectSubtree(TodoItemViewModel node, List<TodoItemViewModel> result)
        {
            result.Add(node);
            foreach (var child in node.SubTasks)
                CollectSubtree(child, result);
        }

        private async System.Threading.Tasks.Task EditTodo(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            try
            {
                var updated = _dialogs.EditTask(vm.Model, Categories.ToList());
                if (updated == null) return;

                vm.Title = updated.Title;
                vm.Description = updated.Description;
                vm.Category = updated.Category;
                vm.Priority = updated.Priority;
                vm.DueDate = updated.DueDate;
                vm.Recurrence = updated.Recurrence;
                vm.Icon = updated.Icon;
                vm.Tags = updated.Tags;
                vm.Model.Attachments = updated.Attachments;
                ApplySubTaskSorting();

                await _todoRepo.SaveChangesAsync();
                await RefreshCategoriesAsync();
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to edit task:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task DeleteTodo(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            var confirmed = _dialogs.Confirm(
                $"Delete \"{vm.Title}\"?" +
                (vm.HasSubTasks ? $"\n\nIts {vm.SubTasks.Count} sub-task(s) will also be deleted." : ""),
                "Confirm Delete");

            if (!confirmed) return;

            try
            {
                var subtree = new List<TodoItemViewModel>();
                CollectSubtree(vm, subtree);
                var snapshot = subtree.Select(s => CloneItem(s.Model)).ToList();

                for (int i = subtree.Count - 1; i >= 0; i--)
                    await _todoRepo.DeleteAsync(subtree[i].Model);

                await _todoRepo.SaveChangesAsync();

                _lastDeletedItems = snapshot;
                UndoDeleteCommand.RaiseCanExecuteChanged();

                DetachEvents(vm);
                AllTodos.Remove(vm);
                await RefreshCategoriesAsync();
                UpdateStatus();

                StatusText += "  ·  Ctrl+Z to undo";
            }
            catch (Exception ex)
            {
                AttachEvents(vm);
                _dialogs.ShowError($"Failed to delete task:\n\n{ex.Message}", "Error");
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

        private async System.Threading.Tasks.Task UndoDelete()
        {
            if (_lastDeletedItems == null || _lastDeletedItems.Count == 0) return;

            try
            {
                var idMap = new Dictionary<int, int>();
                var pending = new List<TodoItem>(_lastDeletedItems);

                // Ids of the rows we are about to re-insert. A child whose parent is NOT in
                // this set still exists in the database, so its original ParentId must be
                // kept instead of being orphaned into a root-level task.
                var restoringIds = new HashSet<int>(pending.Select(i => i.Id));

                var guard = 0;
                while (pending.Count > 0 && guard++ < 100)
                {
                    var batch = pending
                        .Where(i => i.ParentId == null
                                    || !restoringIds.Contains(i.ParentId.Value)
                                    || idMap.ContainsKey(i.ParentId.Value))
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
                _dialogs.ShowError($"Failed to restore:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task ClearCompleted()
        {
            var completedRoots = AllTodos
                .Where(t => t.IsCompleted && !t.IsArchived)
                .ToList();

            // Children ticked off individually under a still-active parent
            var completedChildren = AllTodos
                .Where(t => !t.IsCompleted && !t.IsArchived)
                .SelectMany(t => t.SubTasks)
                .Where(s => s.IsCompleted && !s.IsArchived)
                .ToList();

            var total = completedRoots.Count + completedChildren.Count;
            if (total == 0) return;

            if (!_dialogs.Confirm($"Remove {total} completed task(s)?")) return;

            try
            {
                var nodes = completedRoots
                    .Concat(completedChildren)
                    .SelectMany(t => t.SelfAndDescendants())
                    .ToList();

                var snapshot = nodes.Select(t => CloneItem(t.Model)).ToList();

                await _todoRepo.DeleteRangeAsync(nodes.Select(t => t.Model));
                await _todoRepo.SaveChangesAsync();

                _lastDeletedItems = snapshot;
                UndoDeleteCommand.RaiseCanExecuteChanged();

                foreach (var item in completedRoots)
                {
                    DetachEvents(item);
                    AllTodos.Remove(item);
                }

                foreach (var child in completedChildren)
                {
                    DetachEvents(child);
                    child.ParentVm?.SubTasks.Remove(child);
                    child.ParentVm?.RefreshSubTasks();
                    child.ParentVm = null;
                }

                await RefreshCategoriesAsync();
                UpdateStatus();

                StatusText += "  ·  Ctrl+Z to undo";
            }
            catch (Exception ex)
            {
                foreach (var item in completedRoots)
                    AttachEvents(item);
                foreach (var child in completedChildren)
                    AttachEvents(child);

                _dialogs.ShowError($"Failed to clear completed tasks:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task ToggleFavorite(TodoItemViewModel? vm)
        {
            if (vm == null) return;
            vm.IsFavorite = !vm.IsFavorite;

            try
            {
                await _todoRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to save favorite:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task ArchiveTask(TodoItemViewModel? vm)
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
                _dialogs.ShowError($"Failed to archive:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task UnarchiveTask(TodoItemViewModel? vm)
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
                _dialogs.ShowError($"Failed to unarchive:\n\n{ex.Message}", "Error");
            }
        }

        public async System.Threading.Tasks.Task OpenDetail(TodoItemViewModel? vm)
        {
            if (vm == null) return;

            try
            {
                var updated = _dialogs.ShowTaskDetail(vm.Model, Categories.ToList());
                if (updated == null) return;

                vm.Title = updated.Title;
                vm.Description = updated.Description;
                vm.Category = updated.Category;
                vm.Priority = updated.Priority;
                vm.DueDate = updated.DueDate;
                vm.Recurrence = updated.Recurrence;
                vm.Icon = updated.Icon;
                vm.Tags = updated.Tags;
                vm.Model.Attachments = updated.Attachments;
                ApplySubTaskSorting();

                await _todoRepo.SaveChangesAsync();
                await RefreshCategoriesAsync();
                TodosView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Failed to save task details:\n\n{ex.Message}", "Error");
            }
        }

        private void OpenDashboard() => _dialogs.ShowDashboard(AllTodos.ToList());

        private void OpenThemePicker()
        {
            _dialogs.ShowThemePicker();
            TodosView.Refresh();
        }

        private void ClearSelection()
        {
            foreach (var t in AllTodos.Where(t => t.IsSelected))
                t.IsSelected = false;
        }

        private async System.Threading.Tasks.Task DeleteSelected()
        {
            var selected = SelectedTodos;
            if (selected.Count == 0) return;

            if (!_dialogs.Confirm($"Delete {selected.Count} selected task(s)?", "Confirm Delete"))
                return;

            try
            {
                var snapshot = new List<TodoItem>();
                var subtreeMap = new Dictionary<TodoItemViewModel, List<TodoItemViewModel>>();

                foreach (var vm in selected)
                {
                    var subtree = new List<TodoItemViewModel>();
                    CollectSubtree(vm, subtree);
                    subtreeMap[vm] = subtree;
                    snapshot.AddRange(subtree.Select(s => CloneItem(s.Model)));
                }

                var ordered = selected
                    .SelectMany(vm => subtreeMap[vm])
                    .GroupBy(t => t)
                    .Select(g => g.Key)
                    .ToList();

                for (int i = ordered.Count - 1; i >= 0; i--)
                    await _todoRepo.DeleteAsync(ordered[i].Model);

                await _todoRepo.SaveChangesAsync();

                _lastDeletedItems = snapshot;
                UndoDeleteCommand.RaiseCanExecuteChanged();

                foreach (var vm in selected)
                {
                    DetachEvents(vm);
                    AllTodos.Remove(vm);
                }

                OnPropertyChanged(nameof(HasSelection));
                DeleteSelectedCommand.RaiseCanExecuteChanged();
                await RefreshCategoriesAsync();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                await LoadFromDatabaseAsync();
                _dialogs.ShowError($"Failed to delete selection:\n\n{ex.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task RefreshCategoriesAsync()
        {
            var current = SelectedCategory;
            Categories.Clear();
            Categories.Add("All Categories");

            var categories = AllTodos
                .SelectMany(t => t.SelfAndDescendants())
                .Select(t => t.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct()
                .OrderBy(c => c);

            foreach (var category in categories)
                Categories.Add(category);

            SelectedCategory = Categories.Contains(current) ? current : "All Categories";
        }

        public async System.Threading.Tasks.Task OpenCategoryDialog()
        {
            var result = _dialogs.ShowCategoryDialog(Categories.ToList());
            if (result == null) return;

            if (result.IsDelete)
            {
                await DeleteCategoryAsync(result.CategoryToDelete);
                return;
            }

            var newCategory = result.CategoryName;
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
                .SelectMany(t => t.SelfAndDescendants())
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
            var visible = AllTodos
                .Where(t => !t.IsArchived)
                .SelectMany(t => t.SelfAndDescendants())
                .ToList();

            var total = visible.Count;
            var completed = visible.Count(t => t.IsCompleted);
            var active = total - completed;

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
                DetachEvents(todo);

            // Deliberately not clearing AllTodos: the collection is about to be
            // discarded with this instance, and notifying its CollectionView is a
            // no-op that throws if Dispose ever runs off the view's dispatcher.
        }
    }
}
