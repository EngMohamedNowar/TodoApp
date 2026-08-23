using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class FocusStatsWindow : Window
    {
        public FocusStatsWindow()
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<FocusStatsViewModel>();
        }
    }
}
