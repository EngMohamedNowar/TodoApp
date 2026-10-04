using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Repositories;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp.Tests
{
    /// <summary>
    /// Coverage for the sub-task drag handle: dragging a sibling reorders the
    /// manual list and renumbers it, while priority mode ignores drags so the
    /// priority ordering stays in charge.
    /// </summary>
    public class MainViewModelSubTaskReorderTests : IDisposable
    {
        private readonly string _dbName = $"vm-subreorder-{Guid.NewGuid():N}";

        private TodoDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<TodoDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);

        public void Dispose() { }

        private async Task SeedParentWithThreeChildrenAsync()
        {
            using var db = CreateContext();
            var repo = new TodoRepository(db);

            await repo.AddAsync(new TodoItem { Title = "Parent", SortOrder = 0 });
            await repo.SaveChangesAsync();

            var parentId = (await repo.GetAllAsync()).Single().Id;

            await repo.AddAsync(new TodoItem
            {
                Title = "Low",
                ParentId = parentId,
                SortOrder = 0,
                Priority = PriorityLevel.Low
            });
            await repo.AddAsync(new TodoItem
            {
                Title = "Medium",
                ParentId = parentId,
                SortOrder = 1,
                Priority = PriorityLevel.Medium
            });
            await repo.AddAsync(new TodoItem
            {
                Title = "High",
                ParentId = parentId,
                SortOrder = 2,
                Priority = PriorityLevel.High
            });
            await repo.SaveChangesAsync();
        }

        private async Task<MainViewModel> LoadViewModelAsync()
        {
            var vm = new MainViewModel(new TodoRepository(CreateContext()), Mock.Of<IDialogService>());
            await Task.Delay(200);
            await vm.LoadFromDatabaseAsync();
            return vm;
        }

        [Fact]
        public void ManualMode_ReorderMovesAndRenumbers()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithThreeChildrenAsync();
                var vm = await LoadViewModelAsync();

                var parent = Assert.Single(vm.AllTodos, t => t.Title == "Parent");
                var low = parent.SubTasks.Single(s => s.Title == "Low");
                var high = parent.SubTasks.Single(s => s.Title == "High");

                await vm.ReorderSubTaskAsync(low, high);

                Assert.Equal(new[] { "Medium", "High", "Low" },
                    parent.SubTasks.Select(s => s.Title));
                Assert.Equal(new[] { 0, 1, 2 },
                    parent.SubTasks.Select(s => s.SortOrder));

                vm.Dispose();
            });
        }

        [Fact]
        public void PriorityMode_ReorderIsIgnored()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithThreeChildrenAsync();
                var vm = await LoadViewModelAsync();

                vm.SubTaskSortModeIndex = 1;

                var parent = Assert.Single(vm.AllTodos, t => t.Title == "Parent");
                var low = parent.SubTasks.Single(s => s.Title == "Low");
                var high = parent.SubTasks.Single(s => s.Title == "High");

                await vm.ReorderSubTaskAsync(low, high);

                Assert.Equal(new[] { "High", "Medium", "Low" },
                    parent.SubTasks.Select(s => s.Title));

                vm.Dispose();
            });
        }
    }
}
