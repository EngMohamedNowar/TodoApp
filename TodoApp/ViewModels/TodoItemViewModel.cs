using System;
using System.Collections.ObjectModel;
using System.Linq;
using TodoApp.Models;

namespace TodoApp.ViewModels
{
    /// <summary>
    /// UI-facing wrapper around a TodoItem entity. Keeps the raw
    /// entity reachable (for saving) while exposing bindable,
    /// display-friendly properties to the XAML.
    /// </summary>
    public class TodoItemViewModel : ViewModelBase
    {
        public TodoItem Model { get; }

        public TodoItemViewModel(TodoItem model)
        {
            Model = model;
            SubTasks = new ObservableCollection<TodoItemViewModel>();
        }

        public int Id => Model.Id;

        public string Title
        {
            get => Model.Title;
            set { Model.Title = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => Model.Description;
            set { Model.Description = value; OnPropertyChanged(); }
        }

        public string? Category
        {
            get => Model.Category;
            set { Model.Category = value; OnPropertyChanged(); }
        }

        public PriorityLevel Priority
        {
            get => Model.Priority;
            set { Model.Priority = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriorityText)); }
        }

        public DateTime? DueDate
        {
            get => Model.DueDate;
            set
            {
                Model.DueDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DueDateDisplay));
                OnPropertyChanged(nameof(IsOverdue));
            }
        }

        public RecurrenceType Recurrence
        {
            get => Model.Recurrence;
            set { Model.Recurrence = value; OnPropertyChanged(); OnPropertyChanged(nameof(RecurrenceText)); }
        }

        public event EventHandler? IsCompletedChanged;

        public bool IsCompleted
        {
            get => Model.IsCompleted;
            set
            {
                if (Model.IsCompleted == value) return;
                Model.IsCompleted = value;
                Model.CompletedAt = value ? DateTime.Now : null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOverdue));
                IsCompletedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ObservableCollection<TodoItemViewModel> SubTasks { get; }

        public void AddSubTask(TodoItemViewModel subVm)
        {
            subVm.Model.ParentId = Model.Id;
            SubTasks.Add(subVm);
            RefreshSubTasks();
        }

        public bool HasSubTasks => SubTasks.Count > 0;

        public int CompletedSubTasks => SubTasks.Count(s => s.IsCompleted);

        public double SubTaskProgress =>
            SubTasks.Count == 0 ? 0 : CompletedSubTasks * 100.0 / SubTasks.Count;

        public string SubTaskLabel => $"{CompletedSubTasks}/{SubTasks.Count}";

        public void RefreshSubTasks()
        {
            OnPropertyChanged(nameof(HasSubTasks));
            OnPropertyChanged(nameof(CompletedSubTasks));
            OnPropertyChanged(nameof(SubTaskProgress));
            OnPropertyChanged(nameof(SubTaskLabel));
        }

        /// <summary>
        /// Marks this item completed without raising IsCompletedChanged,
        /// so cascading a parent's completion onto its sub-tasks does not
        /// trigger parallel SaveChanges calls on the shared DbContext.
        /// </summary>
        public void MarkCompletedQuietly()
        {
            if (Model.IsCompleted) return;
            Model.IsCompleted = true;
            Model.CompletedAt = DateTime.Now;
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsOverdue));
        }

        public DateTime CreatedAt => Model.CreatedAt;

        public int SortOrder
        {
            get => Model.SortOrder;
            set { Model.SortOrder = value; OnPropertyChanged(); }
        }

        public string PriorityText => Priority switch
        {
            PriorityLevel.High => "High",
            PriorityLevel.Low => "Low",
            _ => "Medium"
        };

        public string DueDateDisplay => DueDate.HasValue ? DueDate.Value.ToString("dd MMM yyyy") : "No due date";

        public bool IsOverdue => !IsCompleted && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

        public string RecurrenceText => Recurrence switch
        {
            RecurrenceType.Daily => "\u21BB Daily",
            RecurrenceType.Weekly => "\u21BB Weekly",
            RecurrenceType.Monthly => "\u21BB Monthly",
            _ => ""
        };

        public bool IsFavorite
        {
            get => Model.IsFavorite;
            set
            {
                if (Model.IsFavorite == value) return;
                Model.IsFavorite = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FavoriteGlyph));
            }
        }

        public string FavoriteGlyph => IsFavorite ? "\u2605" : "\u2606";

        public bool IsArchived => Model.IsArchived;

        public string? Icon
        {
            get => Model.Icon;
            set { Model.Icon = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayIcon)); }
        }

        public string? DisplayIcon => string.IsNullOrWhiteSpace(Model.Icon) ? null : Model.Icon;

        public string? Tags
        {
            get => Model.Tags;
            set
            {
                Model.Tags = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TagsList));
                OnPropertyChanged(nameof(HasTags));
            }
        }

        public System.Collections.Generic.IReadOnlyList<string> TagsList =>
            string.IsNullOrWhiteSpace(Model.Tags)
                ? Array.Empty<string>()
                : Model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public bool HasTags => TagsList.Count > 0;

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? SelectionChanged;
    }
}
