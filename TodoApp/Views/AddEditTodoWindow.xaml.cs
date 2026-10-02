using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Models;
using TodoApp.ViewModels;

namespace TodoApp.Views
{
    public partial class AddEditTodoWindow : Window
    {
        private readonly bool _isEditing;

        public TodoItem? ResultItem { get; private set; }
        public AddEditTodoViewModel ViewModel { get; }

        public AddEditTodoWindow(bool isSubTask = false, IEnumerable<string>? existingCategories = null)
        {
            InitializeComponent();
            ViewModel = InitializeViewModel(existingCategories);
            ViewModel.SetNewItem(isSubTask);
            _isEditing = false;
            Loaded += OnWindowLoaded;
        }

        public AddEditTodoWindow(TodoItem item, IEnumerable<string>? existingCategories = null)
        {
            InitializeComponent();
            ViewModel = InitializeViewModel(existingCategories);
            ViewModel.SetEditingItem(item);
            _isEditing = true;
            Loaded += OnWindowLoaded;
        }

        private AddEditTodoViewModel InitializeViewModel(IEnumerable<string>? existingCategories)
        {
            var vm = App.Services.GetRequiredService<AddEditTodoViewModel>();
            DataContext = vm;
            vm.OwnerWindow = this;
            vm.SeedCategories(existingCategories ?? Enumerable.Empty<string>());
            return vm;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;

            try
            {
                await ViewModel.LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Couldn't load categories: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            TitleBox.Focus();

            if (_isEditing)
            {
                TitleBox.SelectAll();
            }
        }

        private void CategoryBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Shift+Delete (not plain Delete) to avoid clashing with normal text editing
            if (e.Key != Key.Delete || Keyboard.Modifiers != ModifierKeys.Shift) return;
            if (CategoryBox.SelectedItem == null) return;

            var category = CategoryBox.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(category)) return;

            var result = MessageBox.Show(
                $"Remove category \"{category}\" from this list?",
                "Delete Category",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            ViewModel.Categories.Remove(category);
            ViewModel.SelectedCategory = string.Empty;
            CategoryBox.Text = string.Empty;
            e.Handled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.Title = TitleBox.Text;
                ViewModel.Description = DescriptionBox.Text ?? string.Empty;
                ViewModel.SelectedCategory = CategoryBox.Text?.Trim() ?? string.Empty;
                ViewModel.SelectedPriorityIndex = PriorityBox.SelectedIndex;
                ViewModel.DueDate = DueDatePicker.SelectedDate;
                ViewModel.RecurrenceIndex = RecurrenceBox.SelectedIndex;

                ViewModel.SaveCommand.Execute(null);

                if (ViewModel.DialogResult)
                {
                    ResultItem = ViewModel.ResultItem;
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Couldn't save task: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}