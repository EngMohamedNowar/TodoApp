using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TodoApp.ViewModels;

namespace TodoApp
{
    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;
        private TodoItemViewModel? _draggedItem;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not FrameworkElement handle || handle.DataContext is not TodoItemViewModel vm) return;

            var current = e.GetPosition(null);
            var diff = _dragStartPoint - current;

            if (System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _draggedItem = vm;
                DragDrop.DoDragDrop(handle, vm, DragDropEffects.Move);
            }
        }

        private void Card_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = _draggedItem != null ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Card_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement card && card.DataContext is TodoItemViewModel targetVm
                && _draggedItem != null && !ReferenceEquals(_draggedItem, targetVm)
                && DataContext is MainViewModel viewModel)
            {
                await viewModel.ReorderTodoAsync(_draggedItem, targetVm);
            }

            _draggedItem = null;
            e.Handled = true;
        }

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement card || card.DataContext is not TodoItemViewModel vm) return;
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;

            vm.IsSelected = !vm.IsSelected;
            e.Handled = true;
        }

        private void ManageCategories_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.OpenCategoryDialog();
        }
    }
}
