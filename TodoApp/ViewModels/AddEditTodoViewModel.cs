using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TodoApp.Models;
using TodoApp.Repositories;

namespace TodoApp.ViewModels
{
    public class AddEditTodoViewModel : ViewModelBase
    {
        private readonly ITodoRepository _todoRepo;

        public TodoItem? ResultItem { get; private set; }

        public ObservableCollection<string> Categories { get; } = new();

        private string _headerText = "New Task";
        public string HeaderText
        {
            get => _headerText;
            set => SetField(ref _headerText, value);
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        private string _tags = string.Empty;
        public string Tags
        {
            get => _tags;
            set => SetField(ref _tags, value);
        }

        private string? _icon;
        public string? Icon
        {
            get => _icon;
            set => SetField(ref _icon, value);
        }

        private string _selectedCategory = string.Empty;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetField(ref _selectedCategory, value);
        }

        private int _selectedPriorityIndex = 1;
        public int SelectedPriorityIndex
        {
            get => _selectedPriorityIndex;
            set => SetField(ref _selectedPriorityIndex, value);
        }

        private DateTime? _dueDate = DateTime.Today;
        public DateTime? DueDate
        {
            get => _dueDate;
            set => SetField(ref _dueDate, value);
        }

        private int _recurrenceIndex;
        public int RecurrenceIndex
        {
            get => _recurrenceIndex;
            set => SetField(ref _recurrenceIndex, value);
        }

        public RecurrenceType Recurrence => (RecurrenceType)RecurrenceIndex;

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private Window? _ownerWindow;
        public Window? OwnerWindow
        {
            get => _ownerWindow;
            set => _ownerWindow = value;
        }

        private bool _dialogResult;
        public bool DialogResult
        {
            get => _dialogResult;
            set => _dialogResult = value;
        }

        public AddEditTodoViewModel(ITodoRepository todoRepo)
        {
            _todoRepo = todoRepo;
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public async Task LoadCategoriesAsync()
        {
            var fromTasks = await _todoRepo.GetDistinctCategoriesAsync();

            var all = _seededCategories
                .Concat(fromTasks)
                .Where(c => !string.IsNullOrWhiteSpace(c) && c != "All Categories")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c);

            Categories.Clear();
            foreach (var c in all)
                Categories.Add(c);
        }

        private List<string> _seededCategories = new();

        /// <summary>
        /// Pre-loads the categories currently visible in the main window's
        /// sidebar so newly created (task-less) categories appear too.
        /// Call before LoadCategoriesAsync.
        /// </summary>
        public void SeedCategories(IEnumerable<string> categories)
        {
            _seededCategories = categories?.ToList() ?? new List<string>();
        }

        public void LoadItem(TodoItem item)
        {
            HeaderText = "Edit Task";
            Title = item.Title;
            Description = item.Description ?? string.Empty;
            Tags = item.Tags ?? string.Empty;
            Icon = item.Icon;
            SelectedCategory = item.Category ?? string.Empty;
            DueDate = item.DueDate;

            SelectedPriorityIndex = item.Priority switch
            {
                PriorityLevel.Low => 0,
                PriorityLevel.Medium => 1,
                PriorityLevel.High => 2,
                _ => 1
            };

            RecurrenceIndex = (int)item.Recurrence;
        }

        public void SetNewItem(bool isSubTask = false)
        {
            HeaderText = isSubTask ? "New Sub-Task" : "New Task";
            Tags = string.Empty;
            Icon = null;
            DueDate = DateTime.Today;
            RecurrenceIndex = isSubTask ? 0 : RecurrenceIndex;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Title)) return;

            var category = string.IsNullOrWhiteSpace(SelectedCategory) ? null : SelectedCategory;
            var priority = SelectedPriorityIndex switch
            {
                0 => PriorityLevel.Low,
                1 => PriorityLevel.Medium,
                2 => PriorityLevel.High,
                _ => PriorityLevel.Medium
            };

            if (_editingItem != null)
            {
                _editingItem.Title = Title.Trim();
                _editingItem.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
                _editingItem.Category = category;
                _editingItem.Priority = priority;
                _editingItem.DueDate = DueDate;
                _editingItem.Recurrence = Recurrence;
                _editingItem.Tags = string.IsNullOrWhiteSpace(Tags) ? null : Tags.Trim();
                _editingItem.Icon = string.IsNullOrWhiteSpace(Icon) ? null : Icon;
                ResultItem = _editingItem;
            }
            else
            {
                ResultItem = new TodoItem
                {
                    Title = Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    Category = category,
                    Priority = priority,
                    DueDate = DueDate,
                    CreatedAt = DateTime.Now,
                    IsCompleted = false,
                    Recurrence = Recurrence,
                    Tags = string.IsNullOrWhiteSpace(Tags) ? null : Tags.Trim(),
                    Icon = string.IsNullOrWhiteSpace(Icon) ? null : Icon
                };
            }

            DialogResult = true;
        }

        private TodoItem? _editingItem;

        public void SetEditingItem(TodoItem item)
        {
            _editingItem = item;
            LoadItem(item);
        }

        private void Cancel()
        {
            DialogResult = false;
        }
    }
}
