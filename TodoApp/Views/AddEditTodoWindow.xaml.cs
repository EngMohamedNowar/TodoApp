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
        public TodoItem? ResultItem { get; private set; }
        public AddEditTodoViewModel ViewModel { get; }

        public AddEditTodoWindow(bool isSubTask = false, IEnumerable<string>? existingCategories = null)
        {
            InitializeComponent();
            ViewModel = App.Services.GetRequiredService<AddEditTodoViewModel>();
            DataContext = ViewModel;
            ViewModel.OwnerWindow = this;

            ViewModel.SeedCategories(existingCategories ?? Enumerable.Empty<string>());
            ViewModel.SetNewItem(isSubTask);
            Loaded += OnNewWindowLoaded;
        }

        private async void OnNewWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnNewWindowLoaded;
            await ViewModel.LoadCategoriesAsync();
            HeaderText.Text = "New Task";
            TitleBox.Focus();
        }

        public AddEditTodoWindow(TodoItem item, IEnumerable<string>? existingCategories = null)
        {
            InitializeComponent();
            ViewModel = App.Services.GetRequiredService<AddEditTodoViewModel>();
            DataContext = ViewModel;
            ViewModel.OwnerWindow = this;

            ViewModel.SeedCategories(existingCategories ?? Enumerable.Empty<string>());
            ViewModel.SetEditingItem(item);
            Loaded += OnEditWindowLoaded;
        }

        private async void OnEditWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnEditWindowLoaded;
            await ViewModel.LoadCategoriesAsync();
            HeaderText.Text = "Edit Task";
            TitleBox.Focus();
            TitleBox.SelectAll();
        }

        private void CategoryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
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
            ViewModel.Title = TitleBox.Text;
            ViewModel.Description = DescriptionBox.Text ?? string.Empty;
            ViewModel.SelectedCategory = CategoryBox.Text?.Trim() ?? string.Empty;
            ViewModel.SelectedPriorityIndex = PriorityBox.SelectedIndex;
            ViewModel.DueDate = DueDatePicker.SelectedDate ?? System.DateTime.Today;
            ViewModel.RecurrenceIndex = RecurrenceBox.SelectedIndex;

            ViewModel.SaveCommand.Execute(null);

            if (ViewModel.DialogResult)
            {
                ResultItem = ViewModel.ResultItem;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
