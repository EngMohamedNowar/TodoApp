using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Tests
{
    public class DbContextTests : IDisposable
    {
        private TodoDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TodoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new TodoDbContext(options);
        }

        public void Dispose() { }

        [Fact]
        public async Task CanCreateAndRetrieveTodoItem()
        {
            using var db = CreateContext();

            var item = new TodoItem
            {
                Title = "Test Task",
                Description = "Test Description",
                Category = "Test Category",
                Priority = PriorityLevel.High,
                DueDate = DateTime.Today,
                CreatedAt = DateTime.Now,
                SortOrder = 0
            };

            db.Todos.Add(item);
            await db.SaveChangesAsync();

            var retrieved = await db.Todos.FindAsync(item.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Test Task", retrieved.Title);
            Assert.Equal(PriorityLevel.High, retrieved.Priority);
        }

        [Fact]
        public async Task CanCreateAndRetrieveFocusSession()
        {
            using var db = CreateContext();

            var session = new FocusSession
            {
                StartedAt = DateTime.Now.AddMinutes(-25),
                CompletedAt = DateTime.Now,
                DurationMinutes = 25
            };

            db.FocusSessions.Add(session);
            await db.SaveChangesAsync();

            var retrieved = await db.FocusSessions.FindAsync(session.Id);
            Assert.NotNull(retrieved);
            Assert.Equal(25, retrieved.DurationMinutes);
        }

        [Fact]
        public async Task CanCreateAndRetrieveCategory()
        {
            using var db = CreateContext();

            var category = new Category
            {
                Name = "Work",
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var retrieved = await db.Categories.FindAsync(category.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Work", retrieved.Name);
        }

        [Fact]
        public async Task CanCreateAndRetrievePomodoroSettings()
        {
            using var db = CreateContext();

            var settings = new PomodoroSettingsEntity
            {
                Id = 1,
                WorkMinutes = 30,
                ShortBreakMinutes = 5,
                LongBreakMinutes = 15,
                SessionsBeforeLongBreak = 4
            };

            db.PomodoroSettings.Add(settings);
            await db.SaveChangesAsync();

            var retrieved = await db.PomodoroSettings.FindAsync(1);
            Assert.NotNull(retrieved);
            Assert.Equal(30, retrieved.WorkMinutes);
            Assert.Equal(5, retrieved.ShortBreakMinutes);
        }

        [Fact]
        public void CategoryNameMustBeUnique_ModelConfigured()
        {
            using var db = CreateContext();
            var entityType = db.Model.FindEntityType(typeof(Category));
            var index = entityType?.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "Name"));
            Assert.NotNull(index);
            Assert.True(index!.IsUnique);
        }

        [Fact]
        public void TodoItemTitleIsRequired_ModelConfigured()
        {
            using var db = CreateContext();
            var entityType = db.Model.FindEntityType(typeof(TodoItem));
            var titleProperty = entityType?.FindProperty(nameof(TodoItem.Title));
            Assert.NotNull(titleProperty);
            Assert.False(titleProperty!.IsNullable);
        }

        [Fact]
        public async Task CanFilterTodosByIsCompleted()
        {
            using var db = CreateContext();

            db.Todos.AddRange(
                new TodoItem { Title = "Active", IsCompleted = false },
                new TodoItem { Title = "Done", IsCompleted = true }
            );
            await db.SaveChangesAsync();

            var completed = await db.Todos.Where(t => t.IsCompleted).ToListAsync();
            Assert.Single(completed);
            Assert.Equal("Done", completed[0].Title);

            var active = await db.Todos.Where(t => !t.IsCompleted).ToListAsync();
            Assert.Single(active);
            Assert.Equal("Active", active[0].Title);
        }

        [Fact]
        public async Task CanGetDistinctCategories()
        {
            using var db = CreateContext();

            db.Todos.AddRange(
                new TodoItem { Title = "1", Category = "Work" },
                new TodoItem { Title = "2", Category = "Personal" },
                new TodoItem { Title = "3", Category = "Work" },
                new TodoItem { Title = "4", Category = null }
            );
            await db.SaveChangesAsync();

            var categories = await db.Todos
                .Where(t => !string.IsNullOrWhiteSpace(t.Category))
                .Select(t => t.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            Assert.Equal(2, categories.Count);
            Assert.Equal("Personal", categories[0]);
            Assert.Equal("Work", categories[1]);
        }

        [Fact]
        public async Task SoftDeleteFocusSessionsPreservesStats()
        {
            using var db = CreateContext();

            var sessions = new List<FocusSession>
            {
                new() { StartedAt = DateTime.Today.AddHours(9), CompletedAt = DateTime.Today.AddHours(9).AddMinutes(25), DurationMinutes = 25 },
                new() { StartedAt = DateTime.Today.AddHours(10), CompletedAt = DateTime.Today.AddHours(10).AddMinutes(25), DurationMinutes = 25 }
            };

            db.FocusSessions.AddRange(sessions);
            await db.SaveChangesAsync();

            var visible = await db.FocusSessions.Where(f => !f.IsHidden).ToListAsync();
            Assert.Equal(2, visible.Count);

            foreach (var s in visible)
                s.IsHidden = true;
            await db.SaveChangesAsync();

            var hiddenCount = await db.FocusSessions.CountAsync(f => f.IsHidden);
            Assert.Equal(2, hiddenCount);

            var totalMinutes = await db.FocusSessions.SumAsync(f => f.DurationMinutes);
            Assert.Equal(50, totalMinutes);
        }

        [Fact]
        public async Task EnsureSchemaCreatesAllTables()
        {
            using var db = CreateContext();
            db.Database.EnsureCreated();

            db.Todos.Add(new TodoItem { Title = "Test" });
            db.FocusSessions.Add(new FocusSession { StartedAt = DateTime.Now, CompletedAt = DateTime.Now, DurationMinutes = 25 });
            db.PomodoroSettings.Add(new PomodoroSettingsEntity { Id = 1 });
            db.Categories.Add(new Category { Name = "TestCat" });
            await db.SaveChangesAsync();

            Assert.True(await db.Todos.AnyAsync());
            Assert.True(await db.FocusSessions.AnyAsync());
            Assert.True(await db.PomodoroSettings.AnyAsync());
            Assert.True(await db.Categories.AnyAsync());
        }

        [Fact]
        public async Task TodoItemSortOrderIsUsed()
        {
            using var db = CreateContext();

            db.Todos.AddRange(
                new TodoItem { Title = "C", SortOrder = 3 },
                new TodoItem { Title = "A", SortOrder = 1 },
                new TodoItem { Title = "B", SortOrder = 2 }
            );
            await db.SaveChangesAsync();

            var ordered = await db.Todos.OrderBy(t => t.SortOrder).ToListAsync();

            Assert.Equal("A", ordered[0].Title);
            Assert.Equal("B", ordered[1].Title);
            Assert.Equal("C", ordered[2].Title);
        }
    }
}
