using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.ViewModels;
using TodoApp.Views;

namespace TodoApp.Services
{
    /// <summary>
    /// Default <see cref="IDialogService"/>. Owns every window creation call in the
    /// app, plus the single-instance caching for the timer and dashboard windows.
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        private PomodoroWindow? _timerWindow;
        private DashboardWindow? _dashboardWindow;

        private static Window? Owner => Application.Current?.MainWindow;

        public TodoItem? NewTask(IReadOnlyList<string> categories)
            => ShowEditDialog(new AddEditTodoWindow(existingCategories: categories));

        public TodoItem? NewSubTask(IReadOnlyList<string> categories)
            => ShowEditDialog(new AddEditTodoWindow(isSubTask: true, existingCategories: categories));

        public TodoItem? EditTask(TodoItem item, IReadOnlyList<string> categories)
            => ShowEditDialog(new AddEditTodoWindow(item, existingCategories: categories));

        public TodoItem? ShowTaskDetail(TodoItem item, IReadOnlyList<string> categories)
        {
            var dialog = new TaskDetailWindow(item, categories);
            if (Owner != null) dialog.Owner = Owner;
            return dialog.ShowDialog() == true ? dialog.ResultItem : null;
        }

        private static TodoItem? ShowEditDialog(AddEditTodoWindow dialog)
        {
            if (Owner != null) dialog.Owner = Owner;
            return dialog.ShowDialog() == true ? dialog.ResultItem : null;
        }

        public CategoryDialogResult? ShowCategoryDialog(IReadOnlyList<string> categories)
        {
            var dialog = new AddCategoryWindow(categories);
            if (Owner != null) dialog.Owner = Owner;
            if (dialog.ShowDialog() != true) return null;

            return new CategoryDialogResult
            {
                IsDelete = dialog.IsDelete,
                CategoryName = dialog.CategoryName,
                CategoryToDelete = dialog.CategoryToDelete
            };
        }

        public void ShowThemePicker()
        {
            var dialog = new ThemePickerWindow();
            if (Owner != null) dialog.Owner = Owner;
            dialog.ShowDialog();
        }

        public void ShowTimer()
        {
            if (_timerWindow != null)
            {
                _timerWindow.Activate();
                if (_timerWindow.WindowState == WindowState.Minimized)
                    _timerWindow.WindowState = WindowState.Normal;
                return;
            }

            _timerWindow = new PomodoroWindow();
            if (Owner != null) _timerWindow.Owner = Owner;
            _timerWindow.Closed += (_, _) => _timerWindow = null;
            _timerWindow.Show();
        }

        public void ShowDashboard(IReadOnlyList<TodoItemViewModel> todos)
        {
            if (_dashboardWindow != null)
            {
                _dashboardWindow.Activate();
                return;
            }

            _dashboardWindow = new DashboardWindow(new List<TodoItemViewModel>(todos));
            if (Owner != null) _dashboardWindow.Owner = Owner;
            _dashboardWindow.Closed += (_, _) => _dashboardWindow = null;
            _dashboardWindow.Show();
        }

        public void ShowAgent(ITodoRepository repository, Func<Task> onTasksChanged)
        {
            var window = new AgentWindow(repository, onTasksChanged);
            if (Owner != null) window.Owner = Owner;
            window.Show();
        }

        public bool Confirm(string message, string title = "Confirm")
        {
            var result = Owner == null
                ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(Owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        public void ShowInfo(string message, string title = "Info")
        {
            if (Owner == null)
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string title = "Error")
        {
            if (Owner == null)
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
