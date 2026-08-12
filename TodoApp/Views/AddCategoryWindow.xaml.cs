using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TodoApp.Views
{
    public partial class AddCategoryWindow : Window
    {
        // =========================================================
        // Result
        // =========================================================

        public string CategoryName { get; private set; } = string.Empty;

        public string CategoryToDelete { get; private set; } = string.Empty;

        public bool IsDelete { get; private set; }


        // =========================================================
        // Constructor
        // =========================================================

        public AddCategoryWindow(
            IEnumerable<string>? categories = null)
        {
            InitializeComponent();


            Loaded += (_, _) =>
            {
                CategoryNameBox.Focus();


                if (categories == null)
                    return;


                var categoryList =
                    categories
                        .Where(c =>
                            !string.IsNullOrWhiteSpace(c) &&
                            c != "All Categories" &&
                            c != "Uncategorized")
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();


                CategoryDeleteComboBox.ItemsSource =
                    categoryList;


                if (categoryList.Count > 0)
                {
                    CategoryDeleteComboBox.SelectedIndex = 0;
                }
            };
        }


        // =========================================================
        // Add Category
        // =========================================================

        private void AddButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var name =
                CategoryNameBox.Text.Trim();


            if (string.IsNullOrWhiteSpace(name))
            {
                CategoryNameBox.Focus();
                return;
            }


            CategoryName = name;

            IsDelete = false;

            DialogResult = true;
        }


        // =========================================================
        // Delete Category
        // =========================================================

        private void DeleteCategoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CategoryDeleteComboBox.SelectedItem
                is not string category)
            {
                return;
            }


            var result =
                MessageBox.Show(
                    $"Delete category \"{category}\"?\n\n" +
                    "All tasks using this category will become Uncategorized.",
                    "Delete Category",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);


            if (result != MessageBoxResult.Yes)
                return;


            CategoryToDelete = category;

            IsDelete = true;

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