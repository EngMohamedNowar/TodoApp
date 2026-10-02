using System.Linq;
using TodoApp.Models;
using TodoApp.ViewModels;

namespace TodoApp.Tests
{
    public class SubTaskTreeTests
    {
        [Fact]
        public void AddSubTask_SetsParentVm()
        {
            var parent = new TodoItemViewModel(new TodoItem { Id = 7, Title = "Parent" });
            var sub = new TodoItemViewModel(new TodoItem { Title = "Child" });

            Assert.Null(sub.ParentVm);

            parent.AddSubTask(sub);

            Assert.Same(parent, sub.ParentVm);
            Assert.Equal(7, sub.Model.ParentId);
        }

        [Fact]
        public void RootTask_HasNullParentVm()
        {
            var root = new TodoItemViewModel(new TodoItem { Id = 1, Title = "Root" });

            Assert.Null(root.ParentVm);
        }

        [Fact]
        public void SelfAndDescendants_IncludesEveryLevel()
        {
            var root = new TodoItemViewModel(new TodoItem { Id = 1, Title = "Root" });
            var child = new TodoItemViewModel(new TodoItem { Id = 2, Title = "Child" });
            var grandChild = new TodoItemViewModel(new TodoItem { Id = 3, Title = "GrandChild" });

            root.AddSubTask(child);
            child.AddSubTask(grandChild);

            var titles = root.SelfAndDescendants().Select(t => t.Title).ToList();

            Assert.Equal(new[] { "Root", "Child", "GrandChild" }, titles);
        }

        [Fact]
        public void SelfAndDescendants_SingleForLeaf()
        {
            var leaf = new TodoItemViewModel(new TodoItem { Title = "Leaf" });

            Assert.Same(leaf, Assert.Single(leaf.SelfAndDescendants()));
        }

        [Fact]
        public void SubTaskLabel_TracksChildCompletion()
        {
            var parent = new TodoItemViewModel(new TodoItem { Id = 1, Title = "Parent" });
            var done = new TodoItemViewModel(new TodoItem { Title = "Done", IsCompleted = true });
            var open = new TodoItemViewModel(new TodoItem { Title = "Open" });

            parent.AddSubTask(done);
            parent.AddSubTask(open);

            Assert.Equal("1/2", parent.SubTaskLabel);
            Assert.Equal(50.0, parent.SubTaskProgress, 1);

            open.IsCompleted = true;
            parent.RefreshSubTasks();

            Assert.Equal("2/2", parent.SubTaskLabel);
            Assert.Equal(100.0, parent.SubTaskProgress, 1);
        }

        [Fact]
        public void SubTaskLabel_IsZeroOfOne_BeforeAnyChildIsDone()
        {
            var parent = new TodoItemViewModel(new TodoItem { Id = 1, Title = "Parent" });
            parent.AddSubTask(new TodoItemViewModel(new TodoItem { Title = "Open" }));

            Assert.Equal("0/1", parent.SubTaskLabel);
            Assert.Equal(0.0, parent.SubTaskProgress, 1);
        }
    }
}
