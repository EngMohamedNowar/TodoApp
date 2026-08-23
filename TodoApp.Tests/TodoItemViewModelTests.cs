using TodoApp.Models;
using TodoApp.ViewModels;

namespace TodoApp.Tests
{
    public class TodoItemViewModelTests
    {
        [Fact]
        public void PriorityText_ReturnsCorrectText()
        {
            var high = new TodoItemViewModel(new TodoItem { Priority = PriorityLevel.High });
            Assert.Equal("High", high.PriorityText);

            var medium = new TodoItemViewModel(new TodoItem { Priority = PriorityLevel.Medium });
            Assert.Equal("Medium", medium.PriorityText);

            var low = new TodoItemViewModel(new TodoItem { Priority = PriorityLevel.Low });
            Assert.Equal("Low", low.PriorityText);
        }

        [Fact]
        public void IsCompletedChanged_FiresEvent()
        {
            var vm = new TodoItemViewModel(new TodoItem());
            bool eventFired = false;
            vm.IsCompletedChanged += (_, _) => eventFired = true;

            vm.IsCompleted = true;

            Assert.True(eventFired);
        }

        [Fact]
        public void IsCompletedChanged_DoesNotFireForSameValue()
        {
            var item = new TodoItem { IsCompleted = false };
            var vm = new TodoItemViewModel(item);
            bool eventFired = false;
            vm.IsCompletedChanged += (_, _) => eventFired = true;

            vm.IsCompleted = false;

            Assert.False(eventFired);
        }

        [Fact]
        public void IsOverdue_ReturnsTrue_WhenPastDue()
        {
            var vm = new TodoItemViewModel(new TodoItem
            {
                IsCompleted = false,
                DueDate = DateTime.Today.AddDays(-1)
            });

            Assert.True(vm.IsOverdue);
        }

        [Fact]
        public void IsOverdue_ReturnsFalse_WhenCompleted()
        {
            var vm = new TodoItemViewModel(new TodoItem
            {
                IsCompleted = true,
                DueDate = DateTime.Today.AddDays(-1)
            });

            Assert.False(vm.IsOverdue);
        }

        [Fact]
        public void IsOverdue_ReturnsFalse_WhenNoDueDate()
        {
            var vm = new TodoItemViewModel(new TodoItem
            {
                IsCompleted = false,
                DueDate = null
            });

            Assert.False(vm.IsOverdue);
        }

        [Fact]
        public void DueDateDisplay_ReturnsFormattedDate()
        {
            var date = new DateTime(2024, 6, 15);
            var vm = new TodoItemViewModel(new TodoItem { DueDate = date });

            Assert.Equal("15 Jun 2024", vm.DueDateDisplay);
        }

        [Fact]
        public void DueDateDisplay_ReturnsNoDueDate_WhenNull()
        {
            var vm = new TodoItemViewModel(new TodoItem { DueDate = null });

            Assert.Equal("No due date", vm.DueDateDisplay);
        }

        [Fact]
        public void CompletionSetsCompletedAt()
        {
            var vm = new TodoItemViewModel(new TodoItem());

            vm.IsCompleted = true;

            Assert.NotNull(vm.Model.CompletedAt);
        }

        [Fact]
        public void UncompletionClearsCompletedAt()
        {
            var vm = new TodoItemViewModel(new TodoItem { IsCompleted = true, CompletedAt = DateTime.Now });

            vm.IsCompleted = false;

            Assert.Null(vm.Model.CompletedAt);
        }
    }
}
