using System;

namespace TodoApp.Models
{
    public enum PriorityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// The database entity representing a single task.
    /// This is the pure data model persisted by EF Core.
    /// </summary>
    public class TodoItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Category { get; set; }

        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public DateTime? DueDate { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        /// <summary>Manual display order set by drag-and-drop reordering. Lower sorts first.</summary>
        public int SortOrder { get; set; }
    }
}
