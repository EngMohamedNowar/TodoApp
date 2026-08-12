namespace TodoApp.Models
{
    /// <summary>
    /// Single-row settings table (Id is always 1) holding the user's
    /// customized Pomodoro durations, so they persist between runs.
    /// </summary>
    public class PomodoroSettingsEntity
    {
        public int Id { get; set; }
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int SessionsBeforeLongBreak { get; set; } = 4;
    }
}
