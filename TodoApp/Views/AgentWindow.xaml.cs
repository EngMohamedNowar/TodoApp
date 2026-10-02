using System.Windows;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class AgentWindow : Window
    {
        public AgentWindow(ITodoRepository repo, System.Func<System.Threading.Tasks.Task> onTasksChanged)
        {
            InitializeComponent();

            var viewModel = new AgentViewModel(repo, onTasksChanged);
            viewModel.ScrollToEndRequested += (_, _) =>
            {
                Dispatcher.InvokeAsync(() => ChatScroll.ScrollToEnd());
            };

            DataContext = viewModel;
        }
    }
}
