using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.ViewModels;

namespace TodoApp.Services
{
    /// <summary>
    /// Result of the add/rename-category dialog: either a new category name or a
    /// request to delete an existing one, never both.
    /// </summary>
    public sealed class CategoryDialogResult
    {
        public bool IsDelete { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string CategoryToDelete { get; init; } = string.Empty;
    }

    /// <summary>
    /// Thin seam between the view models and WPF windows. The view models only ever
    /// ask for a result (a task, a boolean, a category name) so they can be unit
    /// tested without spinning up real windows.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Add-task dialog. Returns null when the user cancels.</summary>
        TodoItem? NewTask(IReadOnlyList<string> categories);

        /// <summary>Add sub-task dialog. Returns null when the user cancels.</summary>
        TodoItem? NewSubTask(IReadOnlyList<string> categories);

        /// <summary>Edit-task dialog. Returns null when the user cancels.</summary>
        TodoItem? EditTask(TodoItem item, IReadOnlyList<string> categories);

        /// <summary>Task detail dialog. Returns null when the user cancels.</summary>
        TodoItem? ShowTaskDetail(TodoItem item, IReadOnlyList<string> categories);

        /// <summary>Add / delete category dialog. Returns null when the user cancels.</summary>
        CategoryDialogResult? ShowCategoryDialog(IReadOnlyList<string> categories);

        void ShowThemePicker();

        /// <summary>Non-modal, single-instance Pomodoro timer.</summary>
        void ShowTimer();

        /// <summary>Non-modal, single-instance completion dashboard.</summary>
        void ShowDashboard(IReadOnlyList<TodoItemViewModel> todos);

        /// <summary>Non-modal AI agent chat window.</summary>
        void ShowAgent(ITodoRepository repository, Func<Task> onTasksChanged);

        bool Confirm(string message, string title = "Confirm");

        void ShowInfo(string message, string title = "Info");

        void ShowError(string message, string title = "Error");
    }
}
