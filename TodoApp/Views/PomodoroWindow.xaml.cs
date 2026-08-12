using System;
using System.Windows;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class PomodoroWindow : Window
    {
        public PomodoroViewModel ViewModel { get; }

        public PomodoroWindow()
        {
            InitializeComponent();
            ViewModel = new PomodoroViewModel();
            ViewModel.OwnerWindow = this;
            DataContext = ViewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.StopTimer();
            base.OnClosed(e);
        }
    }
}
