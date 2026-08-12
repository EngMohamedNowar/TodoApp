using System;
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
    }
}
