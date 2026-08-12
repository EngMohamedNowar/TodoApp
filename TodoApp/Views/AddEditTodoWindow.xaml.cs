using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Views
{
    public partial class AddEditTodoWindow : Window
    {
        // =========================================================
        // Database
        // =========================================================

        private readonly TodoDbContext _db;


        // =========================================================
        // Result
        // =========================================================

        public TodoItem? ResultItem { get; private set; }


        // =========================================================
        // Editing Item
        // =========================================================

        private readonly TodoItem? _editingItem;


        // =========================================================
        // New Task Constructor
        // =========================================================

        public AddEditTodoWindow()
        {
            InitializeComponent();

            _db = new TodoDbContext();

            _db.EnsureSchema();

            HeaderText.Text = "New Task";

            // =====================================================
            // Today's Date Automatically
            // =====================================================

            DueDatePicker.SelectedDate = DateTime.Today;


            // =====================================================
            // Load Categories
            // =====================================================

            LoadCategories();


            // =====================================================
            // Focus Title
            // =====================================================

            Loaded += (_, _) =>
            {
                TitleBox.Focus();
            };
        }


        // =========================================================
        // Edit Task Constructor
        // =========================================================

        public AddEditTodoWindow(TodoItem item)
        {
            InitializeComponent();

            _db = new TodoDbContext();

            _db.EnsureSchema();

            _editingItem = item;

            HeaderText.Text = "Edit Task";


            // =====================================================
            // Load Categories
            // =====================================================

            LoadCategories();


            // =====================================================
            // Load Existing Task
            // =====================================================

            LoadItem(item);
        }


        // =========================================================
        // Load Categories
        // =========================================================

        private void LoadCategories()
        {
            CategoryBox.Items.Clear();

            var categories =
                _db.Todos
                    .Where(t =>
                        !string.IsNullOrWhiteSpace(t.Category))
                    .Select(t => t.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();


            foreach (var category in categories)
            {
                CategoryBox.Items.Add(category);
            }
        }


        // =========================================================
        // Load Existing Todo
        // =========================================================

        private void LoadItem(TodoItem item)
        {
            // =====================================================
            // Title
            // =====================================================

            TitleBox.Text =
                item.Title;


            // =====================================================
            // Description
            // =====================================================

            DescriptionBox.Text =
                item.Description;


            // =====================================================
            // Category
            // =====================================================

            if (!string.IsNullOrWhiteSpace(item.Category))
            {
                var existingCategory =
                    CategoryBox.Items
                        .Cast<object>()
                        .FirstOrDefault(x =>
                            string.Equals(
                                x?.ToString(),
                                item.Category,
                                StringComparison.OrdinalIgnoreCase));


                if (existingCategory != null)
                {
                    CategoryBox.SelectedItem =
                        existingCategory;

                    CategoryBox.Text =
                        existingCategory.ToString();
                }
                else
                {
                    // The category may have been removed
                    // from the database list but still exists
                    // on this task.

                    CategoryBox.Items.Add(
                        item.Category);

                    CategoryBox.SelectedItem =
                        item.Category;

                    CategoryBox.Text =
                        item.Category;
                }
            }
            else
            {
                CategoryBox.SelectedIndex = -1;

                CategoryBox.Text = string.Empty;
            }


            // =====================================================
            // Priority
            // =====================================================

            PriorityBox.SelectedIndex =
                item.Priority switch
                {
                    PriorityLevel.Low => 0,

                    PriorityLevel.Medium => 1,

                    PriorityLevel.High => 2,

                    _ => 1
                };


            // =====================================================
            // Due Date
            // =====================================================

            DueDatePicker.SelectedDate =
                item.DueDate ?? DateTime.Today;


            // =====================================================
            // Focus
            // =====================================================

            Loaded += (_, _) =>
            {
                TitleBox.Focus();

                TitleBox.SelectAll();
            };
        }


        // =========================================================
        // Add Category
        // =========================================================

        private void AddCategoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var categoryWindow =
                new AddCategoryWindow
                {
                    Owner = this
                };


            // =====================================================
            // Open Dialog
            // =====================================================

            if (categoryWindow.ShowDialog() != true)
                return;


            // =====================================================
            // Get Category Name
            // =====================================================

            var categoryName =
                categoryWindow.CategoryName?.Trim();


            if (string.IsNullOrWhiteSpace(categoryName))
                return;


            // =====================================================
            // Check Duplicate
            // =====================================================

            var existingCategory =
                CategoryBox.Items
                    .Cast<object>()
                    .FirstOrDefault(x =>
                        string.Equals(
                            x?.ToString(),
                            categoryName,
                            StringComparison.OrdinalIgnoreCase));


            // =====================================================
            // Existing Category
            // =====================================================

            if (existingCategory != null)
            {
                CategoryBox.SelectedItem =
                    existingCategory;

                CategoryBox.Text =
                    existingCategory.ToString();

                return;
            }


            // =====================================================
            // Add New Category
            // =====================================================

            CategoryBox.Items.Add(
                categoryName);


            // =====================================================
            // Select New Category
            // =====================================================

            CategoryBox.SelectedItem =
                categoryName;


            CategoryBox.Text =
                categoryName;


            // =====================================================
            // Open Dropdown
            // =====================================================

            CategoryBox.IsDropDownOpen = true;
        }


        // =========================================================
        // Delete Category From Current List
        // =========================================================

        private void CategoryBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            // Only react to Delete key

            if (e.Key != Key.Delete)
                return;


            // =====================================================
            // Get Selected Category
            // =====================================================

            if (CategoryBox.SelectedItem == null)
                return;


            var category =
                CategoryBox.SelectedItem
                    .ToString();


            if (string.IsNullOrWhiteSpace(category))
                return;


            // =====================================================
            // Confirm Delete
            // =====================================================

            var result =
                MessageBox.Show(
                    $"Remove category \"{category}\" from this list?",
                    "Delete Category",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);


            if (result != MessageBoxResult.Yes)
                return;


            // =====================================================
            // Remove Category
            // =====================================================

            CategoryBox.Items.Remove(
                CategoryBox.SelectedItem);


            // =====================================================
            // Clear Selection
            // =====================================================

            CategoryBox.SelectedIndex = -1;

            CategoryBox.Text = string.Empty;


            e.Handled = true;
        }


        // =========================================================
        // Save
        // =========================================================

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // =====================================================
            // Validate Title
            // =====================================================

            var title =
                TitleBox.Text.Trim();


            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "Please enter a task title.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);


                TitleBox.Focus();

                return;
            }


            // =====================================================
            // Description
            // =====================================================

            var description =
                DescriptionBox.Text?.Trim();


            // =====================================================
            // Category
            // =====================================================

            // IMPORTANT:
            // CategoryBox is now an editable ComboBox.
            // Therefore we read Text instead of only SelectedItem.

            var category =
                CategoryBox.Text?.Trim();


            if (string.IsNullOrWhiteSpace(category))
            {
                category = null;
            }
            else
            {
                // =================================================
                // Make sure typed category exists in the list
                // =================================================

                var existingCategory =
                    CategoryBox.Items
                        .Cast<object>()
                        .FirstOrDefault(x =>
                            string.Equals(
                                x?.ToString(),
                                category,
                                StringComparison.OrdinalIgnoreCase));


                if (existingCategory == null)
                {
                    CategoryBox.Items.Add(category);
                }
                else
                {
                    // Use the existing spelling
                    category =
                        existingCategory.ToString();
                }
            }


            // =====================================================
            // Priority
            // =====================================================

            var priority =
                PriorityBox.SelectedIndex switch
                {
                    0 => PriorityLevel.Low,

                    1 => PriorityLevel.Medium,

                    2 => PriorityLevel.High,

                    _ => PriorityLevel.Medium
                };


            // =====================================================
            // Due Date
            // =====================================================

            var dueDate =
                DueDatePicker.SelectedDate
                ?? DateTime.Today;


            // =====================================================
            // EDIT EXISTING TASK
            // =====================================================

            if (_editingItem != null)
            {
                _editingItem.Title =
                    title;


                _editingItem.Description =
                    description;


                _editingItem.Category =
                    category;


                _editingItem.Priority =
                    priority;


                _editingItem.DueDate =
                    dueDate;


                ResultItem =
                    _editingItem;
            }


            // =====================================================
            // CREATE NEW TASK
            // =====================================================

            else
            {
                ResultItem =
                    new TodoItem
                    {
                        Title = title,

                        Description = description,

                        Category = category,

                        Priority = priority,

                        DueDate = dueDate,

                        CreatedAt = DateTime.Now,

                        IsCompleted = false
                    };
            }


            // =====================================================
            // Close Dialog
            // =====================================================

            DialogResult = true;
        }


        // =========================================================
        // Cancel
        // =========================================================

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}