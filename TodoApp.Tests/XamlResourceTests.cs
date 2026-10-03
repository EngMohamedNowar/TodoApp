using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TodoApp.Tests
{
    public class XamlResourceTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TodoApp.sln")))
                dir = dir.Parent;

            if (dir == null)
                throw new InvalidOperationException("Could not locate repository root from " + AppContext.BaseDirectory);

            return dir.FullName;
        }

        private static XDocument Load(params string[] parts) =>
            XDocument.Load(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

        private static IEnumerable<XElement> Styles(XDocument doc) =>
            doc.Descendants().Where(e => e.Name.LocalName == "Style");

        private static XElement ImplicitStyle(XDocument doc, string targetType) =>
            Styles(doc)
                .FirstOrDefault(s =>
                    s.Attribute("TargetType")?.Value == targetType &&
                    s.Attribute(Xaml + "Key") == null)
            ?? throw new InvalidOperationException(
                $"No implicit (unkeyed) Style TargetType=\"{targetType}\" found.");

        [Fact]
        public void App_Styles_Control_EveryCommonInputControl()
        {
            var doc = Load("TodoApp", "App.xaml");

            foreach (var type in new[] { "TextBox", "ComboBox", "ComboBoxItem", "DatePicker", "DatePickerTextBox", "CheckBox" })
                ImplicitStyle(doc, type);
        }

        [Fact]
        public void App_ComboboxTemplate_ShowsText_ForEditableCombobox()
        {
            var template = ImplicitStyle(doc: Load("TodoApp", "App.xaml"), targetType: "ComboBox")
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "ControlTemplate");

            Assert.NotNull(template);

            var editable = template!.Descendants()
                .FirstOrDefault(e => e.Attribute(Xaml + "Name")?.Value == "PART_EditableTextBox");

            Assert.NotNull(editable);
            Assert.Contains("RelativeSource", editable!.Attribute("Text")?.Value ?? string.Empty, StringComparison.Ordinal);
            Assert.Contains("TemplatedParent", editable.Attribute("Text")?.Value ?? string.Empty, StringComparison.Ordinal);
        }

        [Fact]
        public void App_DatePickerTemplate_UsesDarkCalendarButton()
        {
            var template = ImplicitStyle(doc: Load("TodoApp", "App.xaml"), targetType: "DatePicker")
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "ControlTemplate");

            Assert.NotNull(template);

            var button = template!.Descendants()
                .FirstOrDefault(e => e.Attribute(Xaml + "Name")?.Value == "PART_Button");

            Assert.NotNull(button);
            Assert.Equal("Button", button!.Name.LocalName);

            var glyph = button.Descendants().FirstOrDefault(e => e.Name.LocalName == "TextBlock");
            Assert.NotNull(glyph);
            Assert.Equal("Segoe MDL2 Assets", glyph!.Attribute("FontFamily")?.Value);
        }

        [Fact]
        public void TaskDetailWindow_DoesNotShadow_AppControlStyles()
        {
            var doc = Load("TodoApp", "Views", "TaskDetailWindow.xaml");

            var shadowing = new[] { "DarkTextBoxStyle", "DarkComboBoxStyle", "DarkDatePickerStyle" };

            foreach (var key in shadowing)
                Assert.DoesNotContain(Styles(doc), s => s.Attribute(Xaml + "Key")?.Value == key);

            var text = File.ReadAllText(Path.Combine(RepoRoot(), "TodoApp", "Views", "TaskDetailWindow.xaml"));

            foreach (var key in shadowing)
                Assert.DoesNotContain(text, key, StringComparison.Ordinal);
        }

        [Fact]
        public void TaskDetailWindow_EveryStaticResource_IsDefined()
        {
            var path = Path.Combine(RepoRoot(), "TodoApp", "Views", "TaskDetailWindow.xaml");
            var text = File.ReadAllText(path);

            var referenced = Regex.Matches(text, @"\{StaticResource\s+([^{}]+?)\s*\}")
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            Assert.NotEmpty(referenced);

            var windowDoc = XDocument.Parse(text);
            var appDoc = Load("TodoApp", "App.xaml");

            var available = windowDoc.Descendants()
                .Concat(appDoc.Descendants())
                .Select(e => e.Attribute(Xaml + "Key")?.Value)
                .Where(k => k != null)
                .ToHashSet(StringComparer.Ordinal);

            var missing = referenced.Where(k => !available.Contains(k)).ToList();

            Assert.Empty(missing);
        }

        [Fact]
        public void TaskDetailWindow_EveryGrid_DefinesEnoughRows()
        {
            var doc = Load("TodoApp", "Views", "TaskDetailWindow.xaml");

            foreach (var grid in doc.Descendants().Where(e => e.Name.LocalName == "Grid"))
            {
                var rowCount = grid.Elements().Count(e => e.Name.LocalName == "RowDefinition");
                if (rowCount == 0) continue;

                var usedRows = grid.Elements()
                    .Select(e => (int?)e.Attribute("Grid.Row"))
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .DefaultIfEmpty(-1)
                    .Max();

                Assert.True(rowCount > usedRows,
                    $"Grid has {rowCount} RowDefinitions but a child uses Grid.Row=\"{usedRows}\".");
            }
        }
    }
}
