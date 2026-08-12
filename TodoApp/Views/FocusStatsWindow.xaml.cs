using System.Windows;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class FocusStatsWindow : Window
    {
        public FocusStatsWindow()
        {
            InitializeComponent();
            DataContext = new FocusStatsViewModel();
        }
    }
}
