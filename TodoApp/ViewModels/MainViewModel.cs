using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Views;

namespace TodoApp.ViewModels
{
    public enum TaskFilter
    {
        All,
        Active,
        Completed
    }


    public class MainViewModel : ViewModelBase
    {
        private readonly TodoDbContext _db;


        // =========================================================
        // Collections
        // =========================================================

        public ObservableCollection<TodoItemViewModel> AllTodos { get; }
            = new();


        public ICollectionView TodosView { get; }


        public ObservableCollection<string> Categories { get; }
            = new()
            {
                "All Categories"
            };


        // =========================================================
        // Search
        // =========================================================

        private string _searchText = string.Empty;


        public string SearchText
        {
            get => _searchText;

            set
            {
                if (SetField(
                        ref _searchText,
                        value))
                {
                    TodosView.Refresh();
                }
            }
        }


        // =========================================================
        // Task Filter
        // =========================================================

        private TaskFilter _filter =
            TaskFilter.All;


        public TaskFilter Filter
        {
            get => _filter;

            set
            {
                if (SetField(
                        ref _filter,
                        value))
                {
                    TodosView.Refresh();
                }
            }
        }


        // =========================================================
        // Category Filter
        // =========================================================

        private string _selectedCategory =
            "All Categories";


        public string SelectedCategory
        {
            get => _selectedCategory;

            set
            {
                if (SetField(
                        ref _selectedCategory,
                        value))
                {
                    TodosView.Refresh();
                }
            }
        }


        // =========================================================
        // Status
        // =========================================================

        private string _statusText =
            string.Empty;


        public string StatusText
        {
            get => _statusText;

            set => SetField(
                ref _statusText,
                value);
        }


        private double _completionPercentage;


        public double CompletionPercentage
        {
            get => _completionPercentage;

            set => SetField(
                ref _completionPercentage,
                value);
        }


        private int _completedCount;


        public int CompletedCount
        {
            get => _completedCount;

            set => SetField(
                ref _completedCount,
                value);
        }


        private int _totalCount;


        public int TotalCount
        {
            get => _totalCount;

            set => SetField(
                ref _totalCount,
                value);
        }


        // =========================================================
        // Commands
        // =========================================================

        public RelayCommand AddCommand { get; }

        public RelayCommand EditCommand { get; }

        public RelayCommand DeleteCommand { get; }

        public RelayCommand ClearCompletedCommand { get; }

        public RelayCommand SetFilterCommand { get; }

        public RelayCommand OpenTimerCommand { get; }


        // =========================================================
        // Timer Window
        // =========================================================

        private PomodoroWindow? _timerWindow;


        // =========================================================
        // Constructor
        // =========================================================

        public MainViewModel()
        {
            _db =
                new TodoDbContext();


            _db.EnsureSchema();


            // =====================================================
            // Collection View
            // =====================================================

            TodosView =
                CollectionViewSource
                    .GetDefaultView(AllTodos);


            TodosView.Filter =
                FilterPredicate;


            TodosView.SortDescriptions.Add(
                new SortDescription(
                    nameof(TodoItemViewModel.SortOrder),
                    ListSortDirection.Ascending));


            // =====================================================
            // Commands
            // =====================================================

            AddCommand =
                new RelayCommand(
                    _ => AddTodo());


            EditCommand =
                new RelayCommand(
                    p =>
                        EditTodo(
                            p as TodoItemViewModel));


            DeleteCommand =
                new RelayCommand(
                    p =>
                        DeleteTodo(
                            p as TodoItemViewModel));


            ClearCompletedCommand =
                new RelayCommand(
                    _ =>
                        ClearCompleted());


            SetFilterCommand =
                new RelayCommand(
                    p =>
                    {
                        if (p is string value &&
                            Enum.TryParse<TaskFilter>(
                                value,
                                out var filter))
                        {
                            Filter = filter;
                        }
                    });


            OpenTimerCommand =
                new RelayCommand(
                    _ =>
                        OpenTimer());


            // =====================================================
            // Load Database
            // =====================================================

            LoadFromDatabase();
        }


        // =========================================================
        // Load From Database
        // =========================================================

        private void LoadFromDatabase()
        {
            AllTodos.Clear();


            var todos =
                _db.Todos
                    .OrderBy(
                        t => t.SortOrder)
                    .ThenByDescending(
                        t => t.CreatedAt)
                    .ToList();


            foreach (var item in todos)
            {
                AddToCollection(
                    new TodoItemViewModel(item));
            }


            RefreshCategories();

            UpdateStatus();
        }


        // =========================================================
        // Add To Collection
        // =========================================================

        private void AddToCollection(
            TodoItemViewModel vm)
        {
            vm.IsCompletedChanged +=
                OnItemCompletionChanged;


            AllTodos.Add(vm);
        }


        // =========================================================
        // Reorder Todo
        // =========================================================

        public void ReorderTodo(
            TodoItemViewModel dragged,
            TodoItemViewModel target)
        {
            if (dragged == target)
                return;


            var oldIndex =
                AllTodos.IndexOf(dragged);


            var newIndex =
                AllTodos.IndexOf(target);


            if (oldIndex < 0 ||
                newIndex < 0)
            {
                return;
            }


            AllTodos.Move(
                oldIndex,
                newIndex);


            for (
                int i = 0;
                i < AllTodos.Count;
                i++)
            {
                AllTodos[i].SortOrder = i;
            }


            _db.SaveChanges();


            TodosView.Refresh();
        }


        // =========================================================
        // Completion Changed
        // =========================================================

        private void OnItemCompletionChanged(
            object? sender,
            EventArgs e)
        {
            _db.SaveChanges();


            TodosView.Refresh();


            UpdateStatus();
        }


        // =========================================================
        // Filter
        // =========================================================

        private bool FilterPredicate(
            object obj)
        {
            if (obj is not TodoItemViewModel vm)
                return false;


            // =====================================================
            // Status
            // =====================================================

            if (Filter == TaskFilter.Active &&
                vm.IsCompleted)
            {
                return false;
            }


            if (Filter == TaskFilter.Completed &&
                !vm.IsCompleted)
            {
                return false;
            }


            // =====================================================
            // Category
            // =====================================================

            if (SelectedCategory !=
                "All Categories")
            {
                var category =
                    string.IsNullOrWhiteSpace(
                        vm.Category)
                        ? "Uncategorized"
                        : vm.Category;


                if (category != SelectedCategory)
                    return false;
            }


            // =====================================================
            // Search
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    SearchText))
            {
                var term =
                    SearchText.Trim();


                var inTitle =
                    vm.Title?.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)
                    ?? false;


                var inDescription =
                    vm.Description?.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase)
                    ?? false;


                if (!inTitle &&
                    !inDescription)
                {
                    return false;
                }
            }


            return true;
        }


        // =========================================================
        // Open Pomodoro Timer
        // =========================================================

        private void OpenTimer()
        {
            if (_timerWindow != null)
            {
                _timerWindow.Activate();


                if (_timerWindow.WindowState ==
                    WindowState.Minimized)
                {
                    _timerWindow.WindowState =
                        WindowState.Normal;
                }


                return;
            }


            _timerWindow =
                new PomodoroWindow();


            if (Application.Current.MainWindow != null)
            {
                _timerWindow.Owner =
                    Application.Current.MainWindow;
            }


            _timerWindow.Closed +=
                (_, _) =>
                {
                    _timerWindow = null;
                };


            _timerWindow.Show();
        }


        // =========================================================
        // Add Todo
        // =========================================================

        private void AddTodo()
        {
            var dialog =
                new AddEditTodoWindow();


            if (Application.Current.MainWindow != null)
            {
                dialog.Owner =
                    Application.Current.MainWindow;
            }


            if (dialog.ShowDialog() != true)
                return;


            if (dialog.ResultItem == null)
                return;


            var minOrder =
                AllTodos.Count > 0
                    ? AllTodos.Min(
                        t => t.SortOrder)
                    : 0;


            dialog.ResultItem.SortOrder =
                minOrder - 1;


            _db.Todos.Add(
                dialog.ResultItem);


            _db.SaveChanges();


            var vm =
                new TodoItemViewModel(
                    dialog.ResultItem);


            AddToCollection(vm);


            TodosView.Refresh();


            RefreshCategories();


            UpdateStatus();
        }


        // =========================================================
        // Edit Todo
        // =========================================================

        private void EditTodo(
            TodoItemViewModel? vm)
        {
            if (vm == null)
                return;


            var dialog =
                new AddEditTodoWindow(
                    vm.Model);


            if (Application.Current.MainWindow != null)
            {
                dialog.Owner =
                    Application.Current.MainWindow;
            }


            if (dialog.ShowDialog() != true)
                return;


            if (dialog.ResultItem == null)
                return;


            vm.Title =
                dialog.ResultItem.Title;


            vm.Description =
                dialog.ResultItem.Description;


            vm.Category =
                dialog.ResultItem.Category;


            vm.Priority =
                dialog.ResultItem.Priority;


            vm.DueDate =
                dialog.ResultItem.DueDate;


            _db.SaveChanges();


            RefreshCategories();


            TodosView.Refresh();


            UpdateStatus();
        }


        // =========================================================
        // Delete Todo
        // =========================================================

        private void DeleteTodo(
            TodoItemViewModel? vm)
        {
            if (vm == null)
                return;


            var result =
                MessageBox.Show(
                    $"Delete \"{vm.Title}\"?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);


            if (result !=
                MessageBoxResult.Yes)
            {
                return;
            }


            vm.IsCompletedChanged -=
                OnItemCompletionChanged;


            _db.Todos.Remove(
                vm.Model);


            _db.SaveChanges();


            AllTodos.Remove(vm);


            RefreshCategories();


            UpdateStatus();
        }


        // =========================================================
        // Clear Completed
        // =========================================================

        private void ClearCompleted()
        {
            var completed =
                AllTodos
                    .Where(
                        t => t.IsCompleted)
                    .ToList();


            if (!completed.Any())
                return;


            var result =
                MessageBox.Show(
                    $"Remove {completed.Count} completed task(s)?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);


            if (result !=
                MessageBoxResult.Yes)
            {
                return;
            }


            foreach (var item in completed)
            {
                item.IsCompletedChanged -=
                    OnItemCompletionChanged;


                _db.Todos.Remove(
                    item.Model);


                AllTodos.Remove(item);
            }


            _db.SaveChanges();


            RefreshCategories();


            UpdateStatus();
        }


        // =========================================================
        // Refresh Categories
        // =========================================================

        private void RefreshCategories()
        {
            var current =
                SelectedCategory;


            Categories.Clear();


            Categories.Add(
                "All Categories");


            var categories =
                AllTodos
                    .Select(
                        t =>
                            string.IsNullOrWhiteSpace(
                                t.Category)
                                ? "Uncategorized"
                                : t.Category!)
                    .Distinct()
                    .OrderBy(
                        c => c);


            foreach (var category in categories)
            {
                Categories.Add(category);
            }


            SelectedCategory =
                Categories.Contains(current)
                    ? current
                    : "All Categories";
        }


        // =========================================================
        // Delete Category
        // =========================================================

        private void DeleteCategory(
            string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return;


            if (category == "All Categories")
                return;


            if (category == "Uncategorized")
                return;


            var todosWithCategory =
                AllTodos
                    .Where(
                        t =>
                            string.Equals(
                                t.Category,
                                category,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();


            if (!todosWithCategory.Any())
            {
                RefreshCategories();
                return;
            }


            foreach (var todo in todosWithCategory)
            {
                todo.Category = null;
            }


            _db.SaveChanges();


            RefreshCategories();


            TodosView.Refresh();


            UpdateStatus();
        }


        // =========================================================
        // Update Status
        // =========================================================

        private void UpdateStatus()
        {
            var total =
                AllTodos.Count;


            var active =
                AllTodos.Count(
                    t => !t.IsCompleted);


            var completed =
                total - active;


            StatusText =
                $"{active} active / {total} total";


            TotalCount =
                total;


            CompletedCount =
                completed;


            CompletionPercentage =
                total == 0
                    ? 0
                    : Math.Round(
                        completed * 100.0 / total,
                        1);
        }


        // =========================================================
        // Open Category Dialog
        // =========================================================

        public void OpenCategoryDialog()
        {
            var dialog =
                new AddCategoryWindow(
                    Categories);


            if (Application.Current.MainWindow != null)
            {
                dialog.Owner =
                    Application.Current.MainWindow;
            }


            if (dialog.ShowDialog() != true)
                return;


            // =====================================================
            // Delete Category
            // =====================================================

            if (dialog.IsDelete)
            {
                DeleteCategory(
                    dialog.CategoryToDelete);

                return;
            }


            // =====================================================
            // Add Category
            // =====================================================

            var newCategory =
                dialog.CategoryName;


            if (string.IsNullOrWhiteSpace(
                    newCategory))
            {
                return;
            }


            // Category عندك ليست Entity مستقلة.
            // لذلك لا نحتاج Save هنا.
            //
            // سيتم استخدامها عند إضافة / تعديل Task.
            //
            // نضيفها للـ Collection مؤقتًا لكي تظهر في الـ UI.

            if (!Categories.Contains(
                    newCategory))
            {
                Categories.Add(
                    newCategory);
            }


            SelectedCategory =
                newCategory;


            TodosView.Refresh();
        }
    }
}