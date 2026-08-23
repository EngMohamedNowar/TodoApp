using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Repositories;

namespace TodoApp.Tests
{
    public class RepositoryTests : IDisposable
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
        public async Task TodoRepository_CanAddAndRetrieve()
        {
            using var db = CreateContext();
            var repo = new TodoRepository(db);

            var item = new TodoItem { Title = "Test", SortOrder = 0 };
            await repo.AddAsync(item);
            await repo.SaveChangesAsync();

            var all = await repo.GetAllAsync();
            Assert.Single(all);
            Assert.Equal("Test", all[0].Title);
        }

        [Fact]
        public async Task TodoRepository_CanDelete()
        {
            using var db = CreateContext();
            var repo = new TodoRepository(db);

            var item = new TodoItem { Title = "Delete Me" };
            await repo.AddAsync(item);
            await repo.SaveChangesAsync();

            await repo.DeleteAsync(item);
            await repo.SaveChangesAsync();

            var all = await repo.GetAllAsync();
            Assert.Empty(all);
        }

        [Fact]
        public async Task TodoRepository_GetDistinctCategories()
        {
            using var db = CreateContext();
            var repo = new TodoRepository(db);

            await repo.AddAsync(new TodoItem { Title = "1", Category = "Work" });
            await repo.AddAsync(new TodoItem { Title = "2", Category = "Personal" });
            await repo.AddAsync(new TodoItem { Title = "3", Category = "Work" });
            await repo.SaveChangesAsync();

            var categories = await repo.GetDistinctCategoriesAsync();
            Assert.Equal(2, categories.Count);
            Assert.Contains("Personal", categories);
            Assert.Contains("Work", categories);
        }

        [Fact]
        public async Task FocusSessionRepository_GetTodayCount()
        {
            using var db = CreateContext();
            var repo = new FocusSessionRepository(db);

            await repo.AddAsync(new FocusSession
            {
                StartedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                DurationMinutes = 25
            });
            await repo.SaveChangesAsync();

            var count = await repo.GetTodayCountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task FocusSessionRepository_GetTodayHours()
        {
            using var db = CreateContext();
            var repo = new FocusSessionRepository(db);

            await repo.AddAsync(new FocusSession
            {
                StartedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                DurationMinutes = 30
            });
            await repo.SaveChangesAsync();

            var hours = await repo.GetTodayHoursAsync();
            Assert.Equal(0.5, hours);
        }

        [Fact]
        public async Task FocusSessionRepository_SoftDeleteAllVisible()
        {
            using var db = CreateContext();
            var repo = new FocusSessionRepository(db);

            await repo.AddAsync(new FocusSession
            {
                StartedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                DurationMinutes = 25
            });
            await repo.SaveChangesAsync();

            await repo.SoftDeleteAllVisibleAsync();
            await repo.SaveChangesAsync();

            var visible = await repo.GetVisibleAsync();
            Assert.Empty(visible);

            var all = await db.FocusSessions.ToListAsync();
            Assert.Single(all);
            Assert.True(all[0].IsHidden);
        }

        [Fact]
        public async Task PomodoroSettingsRepository_GetOrCreate()
        {
            using var db = CreateContext();
            var repo = new PomodoroSettingsRepository(db);

            var settings = await repo.GetOrCreateAsync();
            Assert.NotNull(settings);
            Assert.Equal(25, settings.WorkMinutes);
        }

        [Fact]
        public async Task CategoryRepository_CanAddAndCheck()
        {
            using var db = CreateContext();
            var repo = new CategoryRepository(db);

            await repo.AddAsync(new Category { Name = "New Category" });
            await repo.SaveChangesAsync();

            Assert.True(await repo.ExistsAsync("New Category"));
            Assert.False(await repo.ExistsAsync("Nonexistent"));
        }
    }
}
