using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TodoApp.Models;

namespace TodoApp.Views
{
    public partial class TaskDetailWindow : Window
    {
        private static readonly string[] IconChoices =
        {
            "", "\uD83D\uDCCB", "\uD83D\uDE80", "\uD83D\uDD25", "\u2B50",
            "\uD83C\uDFAF", "\uD83E\uDDED", "\uD83D\uDCBC", "\uD83C\uDFE0",
            "\uD83D\uDCF1", "\uD83C\uDF93", "\uD83C\uDFE5", "\uD83D\uDE97", "\uD83C\uDF89"
        };

        public TodoItem? ResultItem { get; private set; }

        private readonly TodoItem _item;
        private readonly ObservableCollection<string> _attachments = new();
        private readonly List<string> _removedAttachments = new();

        public TaskDetailWindow(TodoItem item, IEnumerable<string>? categories = null)
        {
            InitializeComponent();

            _item = item;

            foreach (var choice in IconChoices)
                IconBox.Items.Add(choice);
            IconBox.SelectedIndex = Math.Max(0, Array.IndexOf(IconChoices, item.Icon ?? ""));

            TitleBox.Text = item.Title;
            DescriptionBox.Text = item.Description ?? string.Empty;
            TagsBox.Text = item.Tags ?? string.Empty;

            CategoryBox.Items.Add(string.Empty);
            if (categories != null)
                foreach (var c in categories.Where(c => c != "All Categories" && !string.IsNullOrWhiteSpace(c)))
                    CategoryBox.Items.Add(c);
            CategoryBox.SelectedIndex = 0;
            var existingCategory = item.Category ?? string.Empty;
            if (CategoryBox.Items.Contains(existingCategory))
                CategoryBox.SelectedItem = existingCategory;
            else if (!string.IsNullOrWhiteSpace(existingCategory))
                CategoryBox.Text = existingCategory;

            PriorityBox.SelectedIndex = item.Priority switch
            {
                PriorityLevel.Low => 0,
                PriorityLevel.Medium => 1,
                PriorityLevel.High => 2,
                _ => 1
            };

            DueDatePicker.SelectedDate = item.DueDate;
            RecurrenceBox.SelectedIndex = Math.Clamp((int)item.Recurrence, 0, 3);

            CreatedText.Text = $"Created {item.CreatedAt:dd MMM yyyy HH:mm}";
            CompletedText.Text = item.CompletedAt.HasValue ? $"· Completed {item.CompletedAt:dd MMM HH:mm}" : "";

            foreach (var path in ParseAttachments(item.Attachments))
                _attachments.Add(path);
            AttachmentsList.ItemsSource = _attachments;

            Loaded += (_, _) => TitleBox.Focus();
        }

        private static List<string> ParseAttachments(string? stored) =>
            string.IsNullOrWhiteSpace(stored)
                ? new List<string>()
                : stored.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Attach file",
                CheckFileExists = true,
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            foreach (var file in dialog.FileNames)
                if (!_attachments.Contains(file))
                    _attachments.Add(file);
        }

        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not string path) return;

            _attachments.Remove(path);

            if (_item.Id != 0)
                _removedAttachments.Add(path);
        }

        private void AttachmentsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AttachmentsList.SelectedItem is not string path) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open file:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var title = TitleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Title cannot be empty.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultItem = new TodoItem
            {
                Id = _item.Id,
                Title = title,
                Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim(),
                Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? null : CategoryBox.Text.Trim(),
                Priority = (PriorityLevel)Math.Max(0, PriorityBox.SelectedIndex),
                DueDate = DueDatePicker.SelectedDate,
                IsCompleted = _item.IsCompleted,
                CreatedAt = _item.CreatedAt,
                CompletedAt = _item.CompletedAt,
                SortOrder = _item.SortOrder,
                ParentId = _item.ParentId,
                Recurrence = (RecurrenceType)Math.Clamp(RecurrenceBox.SelectedIndex, 0, 3),
                Icon = IconBox.SelectedItem is string icon && icon.Length > 0 ? icon : null,
                Tags = string.IsNullOrWhiteSpace(TagsBox.Text) ? null : TagsBox.Text.Trim(),
                IsFavorite = _item.IsFavorite,
                IsArchived = _item.IsArchived,
                Attachments = _attachments.Count == 0 ? null : string.Join("\n", _attachments)
            };

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
