using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Repositories;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow(List<TodoItemViewModel> todos)
        {
            InitializeComponent();

            IFocusSessionRepository? sessionRepo = null;
            try
            {
                sessionRepo = App.Services.GetRequiredService<IFocusSessionRepository>();
            }
            catch
            {
                // dashboard still works without focus stats
            }

            DataContext = new DashboardViewModel(todos, sessionRepo);
        }
    }
}
