using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
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
    /// Runs a test body on a dedicated STA thread with a pumping WPF Dispatcher.
    /// <see cref="MainViewModel"/> creates a <c>CollectionView</c> over its task
    /// collection in its constructor, and WPF only allows that collection to be
    /// mutated from the dispatcher thread that created the view — so the whole
    /// test has to live there.
    /// </summary>
    internal static class StaTestRunner
    {
        public static void Run(Func<Task> action)
        {
            ExceptionDispatchInfo? failure = null;

            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

                try
                {
                    var frame = new DispatcherFrame();

                    action().ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            failure = ExceptionDispatchInfo.Capture(
                                t.Exception?.InnerException ?? t.Exception!);
                        frame.Continue = false;
                    }, TaskScheduler.Default);

                    Dispatcher.PushFrame(frame);
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            if (!thread.Join(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("The STA test thread did not finish in time.");

            failure?.Throw();
        }
    }

    /// <summary>
    /// Regression coverage for the "sub-tasks don't save" bug: ticking a child's
    /// checkbox used to mutate the in-memory model without ever reaching the
    /// database, because sub-task view models were attached to their parent's
    /// collection without the completion event handlers.
    /// </summary>
    public class MainViewModelSubTaskTests : IDisposable
    {
        private readonly string _dbName = $"vm-subtask-{Guid.NewGuid():N}";

        private TodoDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<TodoDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);

        public void Dispose() { }

        private async Task SeedParentWithChildAsync()
        {
            using var db = CreateContext();
            var repo = new TodoRepository(db);

            await repo.AddAsync(new TodoItem { Title = "Parent", SortOrder = 0 });
            await repo.SaveChangesAsync();

            var parentId = (await repo.GetAllAsync()).Single().Id;

            await repo.AddAsync(new TodoItem { Title = "Child", ParentId = parentId, SortOrder = 0 });
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
        public void LoadFromDatabaseAsync_BuildsSubTaskTree()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithChildAsync();
                var vm = await LoadViewModelAsync();

                var parentVm = Assert.Single(vm.AllTodos, t => t.Title == "Parent");
                var childVm = Assert.Single(parentVm.SubTasks);

                Assert.Equal("Child", childVm.Title);
                Assert.Same(parentVm, childVm.ParentVm);
                Assert.Equal("0/1", parentVm.SubTaskLabel);

                vm.Dispose();
            });
        }

        [Fact]
        public void TickingSubTask_PersistsCompletion_AndRefreshesParentProgress()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithChildAsync();
                var vm = await LoadViewModelAsync();

                var parentVm = Assert.Single(vm.AllTodos, t => t.Title == "Parent");
                var childVm = Assert.Single(parentVm.SubTasks);

                childVm.IsCompleted = true;
                await Task.Delay(300);

                Assert.Equal("1/1", parentVm.SubTaskLabel);

                using var verify = CreateContext();
                var stored = await verify.Todos.SingleAsync(t => t.Title == "Child");
                Assert.True(stored.IsCompleted);

                vm.Dispose();
            });
        }

        [Fact]
        public void CompletingParent_MarksChildrenCompleted_AndPersists()
        {
            StaTestRunner.Run(async () =>
            {
                await SeedParentWithChildAsync();
                var vm = await LoadViewModelAsync();

                var parentVm = Assert.Single(vm.AllTodos, t => t.Title == "Parent");
                parentVm.IsCompleted = true;
                await Task.Delay(300);

                Assert.True(parentVm.SubTasks.Single().IsCompleted);
                Assert.Equal("1/1", parentVm.SubTaskLabel);

                using var verify = CreateContext();
                var stored = await verify.Todos.SingleAsync(t => t.Title == "Child");
                Assert.True(stored.IsCompleted);

                vm.Dispose();
            });
        }
    }
}
