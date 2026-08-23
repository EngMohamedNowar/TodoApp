using System;
using System.IO;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp.Tests
{
    public class NewFeaturesTests : IDisposable
    {
        private readonly string _tempDir;

        public NewFeaturesTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"todoapp-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            SettingsStore.UseDirectoryForTests(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public void RecurrenceType_HasExpectedValues()
        {
            Assert.Equal(0, (int)RecurrenceType.None);
            Assert.Equal(1, (int)RecurrenceType.Daily);
            Assert.Equal(2, (int)RecurrenceType.Weekly);
            Assert.Equal(3, (int)RecurrenceType.Monthly);
        }

        [Fact]
        public void TodoItem_ParentId_DefaultsToNull()
        {
            var item = new TodoItem();
            Assert.Null(item.ParentId);
        }

        [Fact]
        public void TodoItem_Recurrence_DefaultsToNone()
        {
            var item = new TodoItem();
            Assert.Equal(RecurrenceType.None, item.Recurrence);
        }

        [Fact]
        public void TodoItemViewModel_AddSubTask_SetsParentId()
        {
            var parent = new TodoItemViewModel(new TodoItem { Id = 5, Title = "Parent" });
            var sub = new TodoItemViewModel(new TodoItem { Title = "Child" });

            parent.AddSubTask(sub);

            Assert.Equal(5, sub.Model.ParentId);
            Assert.Single(parent.SubTasks);
            Assert.True(parent.HasSubTasks);
        }

        [Fact]
        public void TodoItemViewModel_SubTaskProgress_CalculatesCorrectly()
        {
            var parent = new TodoItemViewModel(new TodoItem());
            parent.AddSubTask(new TodoItemViewModel(new TodoItem { IsCompleted = true }));
            parent.AddSubTask(new TodoItemViewModel(new TodoItem()));
            parent.AddSubTask(new TodoItemViewModel(new TodoItem()));

            Assert.Equal(3, parent.SubTasks.Count);
            Assert.Equal(1, parent.CompletedSubTasks);
            Assert.Equal("1/3", parent.SubTaskLabel);
            Assert.Equal(33.33, parent.SubTaskProgress, 2);
        }

        [Fact]
        public void TodoItemViewModel_RecurrenceText_ReturnsLabels()
        {
            Assert.Equal("", new TodoItemViewModel(new TodoItem()).RecurrenceText);
            Assert.Contains("Daily", new TodoItemViewModel(new TodoItem { Recurrence = RecurrenceType.Daily }).RecurrenceText);
            Assert.Contains("Weekly", new TodoItemViewModel(new TodoItem { Recurrence = RecurrenceType.Weekly }).RecurrenceText);
            Assert.Contains("Monthly", new TodoItemViewModel(new TodoItem { Recurrence = RecurrenceType.Monthly }).RecurrenceText);
        }

        [Theory]
        [InlineData(RecurrenceType.Daily)]
        [InlineData(RecurrenceType.Weekly)]
        [InlineData(RecurrenceType.Monthly)]
        public void TaskSortMode_MapsToEnum(RecurrenceType type)
        {
            // verifies enum roundtrip used by the recurrence ComboBox index binding
            var index = (int)type;
            Assert.Equal(type, (RecurrenceType)index);
        }

        [Fact]
        public void TodoItem_NewFields_DefaultCorrectly()
        {
            var item = new TodoItem();
            Assert.Null(item.Tags);
            Assert.Null(item.Icon);
            Assert.False(item.IsFavorite);
            Assert.False(item.IsArchived);
            Assert.Null(item.Attachments);
        }

        [Fact]
        public void TodoItemViewModel_TagsList_SplitsCommaSeparated()
        {
            var vm = new TodoItemViewModel(new TodoItem { Tags = "work, urgent, home" });

            Assert.Equal(3, vm.TagsList.Count);
            Assert.Equal("work", vm.TagsList[0]);
            Assert.Equal("urgent", vm.TagsList[1]);
            Assert.Equal("home", vm.TagsList[2]);
            Assert.True(vm.HasTags);
        }

        [Fact]
        public void TodoItemViewModel_TagsList_EmptyWhenNull()
        {
            var vm = new TodoItemViewModel(new TodoItem { Tags = null });
            Assert.Empty(vm.TagsList);
            Assert.False(vm.HasTags);
        }

        [Fact]
        public void TodoItemViewModel_FavoriteGlyph_Toggles()
        {
            var vm = new TodoItemViewModel(new TodoItem());
            Assert.Equal("\u2606", vm.FavoriteGlyph);

            vm.IsFavorite = true;
            Assert.Equal("\u2605", vm.FavoriteGlyph);
        }

        [Fact]
        public void TodoItemViewModel_DisplayIcon_NullWhenEmpty()
        {
            var vm = new TodoItemViewModel(new TodoItem { Icon = " " });
            Assert.Null(vm.DisplayIcon);

            vm.Icon = "\uD83D\uDE80";
            Assert.Equal("\uD83D\uDE80", vm.DisplayIcon);
        }

        [Fact]
        public void TodoItemViewModel_IsSelected_RaisesSelectionChanged()
        {
            var vm = new TodoItemViewModel(new TodoItem());
            bool fired = false;
            vm.SelectionChanged += (_, _) => fired = true;

            vm.IsSelected = true;
            Assert.True(fired);

            fired = false;
            vm.IsSelected = true;
            Assert.False(fired);
        }

        [Fact]
        public void SettingsStore_SaveAndLoadRoundTrip()
        {
            var prefs = new SettingsStore.AppPreferences { AccentColor = "#FF6B6B" };
            SettingsStore.Save(prefs);

            var loaded = SettingsStore.Load();
            Assert.Equal("#FF6B6B", loaded.AccentColor);
        }

        [Fact]
        public void SettingsStore_Load_ReturnsDefaultWhenMissing()
        {
            var loaded = SettingsStore.Load();
            Assert.NotNull(loaded);
            Assert.False(string.IsNullOrWhiteSpace(loaded.AccentColor));
        }

        [Fact]
        public void MarkCompletedQuietly_DoesNotFireEvent()
        {
            var vm = new TodoItemViewModel(new TodoItem());
            bool eventFired = false;
            vm.IsCompletedChanged += (_, _) => eventFired = true;

            vm.MarkCompletedQuietly();

            Assert.True(vm.IsCompleted);
            Assert.NotNull(vm.Model.CompletedAt);
            Assert.False(eventFired);
        }

        [Fact]
        public void MarkCompletedQuietly_NoOpWhenAlreadyCompleted()
        {
            var originalTime = DateTime.Now.AddHours(-1);
            var vm = new TodoItemViewModel(new TodoItem { IsCompleted = true, CompletedAt = originalTime });

            vm.MarkCompletedQuietly();

            Assert.Equal(originalTime, vm.Model.CompletedAt);
        }

        [Fact]
        public void DashboardViewModel_ExcludesArchivedFromStats()
        {
            var parent1 = new TodoItemViewModel(new TodoItem { Title = "A", IsCompleted = true });
            var parent2 = new TodoItemViewModel(new TodoItem { Title = "B" });
            parent2.Model.IsArchived = true;
            var parent3 = new TodoItemViewModel(new TodoItem { Title = "C" });
            parent3.Model.IsFavorite = true;

            var dashboard = new DashboardViewModel(
                new System.Collections.Generic.List<TodoItemViewModel> { parent1, parent2, parent3 });

            var totalCard = dashboard.StatCards.First(c => c.Label == "Total tasks");
            Assert.Equal("2", totalCard.Value);

            var starredCard = dashboard.StatCards.First(c => c.Label == "Starred");
            Assert.Equal("1", starredCard.Value);
        }

        [Fact]
        public void DashboardViewModel_CategoryBreakdown_IgnoresArchived()
        {
            var t1 = new TodoItemViewModel(new TodoItem { Title = "A", Category = "Work" });
            var t2 = new TodoItemViewModel(new TodoItem { Title = "B", Category = "Work" });
            t2.Model.IsArchived = true;
            var t3 = new TodoItemViewModel(new TodoItem { Title = "C", Category = null });

            var dashboard = new DashboardViewModel(
                new System.Collections.Generic.List<TodoItemViewModel> { t1, t2, t3 });

            Assert.Equal(2, dashboard.CategoryStats.Count);
            var work = dashboard.CategoryStats.First(c => c.Name == "Work");
            Assert.Equal(1, work.Total);
        }
    }
}
