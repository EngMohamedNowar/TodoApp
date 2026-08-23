using System;

namespace TodoApp.Models
{
    public enum PriorityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum RecurrenceType
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3
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

        /// <summary>Id of the parent task when this item is a sub-task. Null for top-level tasks.</summary>
        public int? ParentId { get; set; }

        /// <summary>When set, completing this task automatically creates the next occurrence.</summary>
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

        /// <summary>Comma-separated tag names (Notion-style labels).</summary>
        public string? Tags { get; set; }

        /// <summary>Emoji shown next to the title. Null hides it.</summary>
        public string? Icon { get; set; }

        /// <summary>Starred / favorite flag.</summary>
        public bool IsFavorite { get; set; }

        /// <summary>Archived tasks are hidden from the main list but kept in the archive.</summary>
        public bool IsArchived { get; set; }

        /// <summary>Newline-separated absolute file paths attached to this task.</summary>
        public string? Attachments { get; set; }
    }
}
