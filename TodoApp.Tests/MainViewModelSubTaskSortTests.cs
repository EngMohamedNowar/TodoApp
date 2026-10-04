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
    /// Coverage for the sidebar's sub-task ordering control: the list can be
    /// kept in manual (insertion) order or re-ordered by priority, and flipping
    /// the mode back restores the manual order.
    /// </summary>
    public class MainViewModelSubTaskSortTests : IDisposable
    {
        private readonly string _dbName = $"vm-subsort-{Guid.NewGuid():N}";

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
        public void PriorityMode_MovesHighestPriorityFirst()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithThreeChildrenAsync();
                var vm = await LoadViewModelAsync();

                var parent = Assert.Single(vm.AllTodos, t => t.Title == "Parent");
                Assert.Equal(new[] { "Low", "Medium", "High" },
                    parent.SubTasks.Select(s => s.Title));

                vm.SubTaskSortModeIndex = 1;

                Assert.Equal(new[] { "High", "Medium", "Low" },
                    parent.SubTasks.Select(s => s.Title));

                vm.Dispose();
            });
        }

        [Fact]
        public void ManualMode_RestoresSortOrderSequence()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithThreeChildrenAsync();
                var vm = await LoadViewModelAsync();

                var parent = Assert.Single(vm.AllTodos, t => t.Title == "Parent");

                vm.SubTaskSortModeIndex = 1;
                Assert.Equal(new[] { "High", "Medium", "Low" },
                    parent.SubTasks.Select(s => s.Title));

                vm.SubTaskSortModeIndex = 0;
                Assert.Equal(new[] { "Low", "Medium", "High" },
                    parent.SubTasks.Select(s => s.Title));

                vm.Dispose();
            });
        }
    }
}
