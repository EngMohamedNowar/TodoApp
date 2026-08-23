using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class PomodoroWindow : Window
    {
        public PomodoroViewModel ViewModel { get; }

        public PomodoroWindow()
        {
            InitializeComponent();
            ViewModel = App.Services.GetRequiredService<PomodoroViewModel>();
            ViewModel.OwnerWindow = this;
            DataContext = ViewModel;

            Closing += PomodoroWindow_Closing;
        }

        private void PomodoroWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (ViewModel.IsRunning)
            {
                var result = MessageBox.Show(
                    "A focus session is currently running.\n\nAre you sure you want to close the timer?\nYour current session progress will be saved.",
                    "Session in Progress",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            ViewModel.StopTimer();
            Closing -= PomodoroWindow_Closing;
        }
    }
}
