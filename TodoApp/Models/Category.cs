using System;

namespace TodoApp.Models
{
    /// <summary>
    /// Represents a user-defined category for organizing tasks.
    /// </summary>
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
