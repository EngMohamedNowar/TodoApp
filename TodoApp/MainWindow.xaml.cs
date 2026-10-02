using System;
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

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _draggedItem = vm;
                try
                {
                    DragDrop.DoDragDrop(handle, vm, DragDropEffects.Move);
                }
                finally
                {
                    // DoDragDrop blocks until the drag ends however it ends
                    // (successful drop, drop outside any target, or Escape).
                    // Clearing here guarantees no stale _draggedItem survives
                    // into the next click, even if Card_Drop never fired.
                    _draggedItem = null;
                }
            }
        }

        private void Card_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = _draggedItem != null ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Card_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (sender is not FrameworkElement card || card.DataContext is not TodoItemViewModel targetVm
                || _draggedItem == null || ReferenceEquals(_draggedItem, targetVm)
                || DataContext is not MainViewModel viewModel)
            {
                return;
            }

            // Capture locally: DoDragDrop's finally block may clear the shared
            // field as soon as this method yields at the first await.
            var draggedItem = _draggedItem;

            try
            {
                await viewModel.ReorderTodoAsync(draggedItem, targetVm);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Couldn't reorder tasks: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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
                _ = vm.OpenCategoryDialog();
        }
    }
}