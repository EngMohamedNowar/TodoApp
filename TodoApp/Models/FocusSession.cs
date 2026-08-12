using System;

namespace TodoApp.Models
{
    /// <summary>
    /// A single completed Pomodoro focus (work) session, persisted so
    /// history survives across days even though "today's" counters
    /// are recomputed live from this table.
    /// </summary>
    public class FocusSession
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public int DurationMinutes { get; set; }

        /// <summary>
        /// When true, the session is excluded from the History tab (soft delete)
        /// but still counted in Summary stats (hours, streak, days accessed)
        /// so those numbers stay accurate after "Clear History".
        /// </summary>
        public bool IsHidden { get; set; }
    }
}