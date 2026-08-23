using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TodoApp.ViewModels
{
    /// <summary>
    /// Generic ICommand implementation used across the app to bind
    /// buttons/menu items directly to view model methods.
    /// Supports both synchronous and asynchronous execute delegates.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Action<object?>? _executeSync;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _executeSync = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _executeAsync = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter)
        {
            if (_executeAsync != null)
            {
                try
                {
                    await _executeAsync(parameter);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"An error occurred:\n\n{ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                _executeSync?.Invoke(parameter);
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
