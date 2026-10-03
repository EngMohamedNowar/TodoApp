namespace TodoApp.Helpers
{
    /// <summary>
    /// The emoji choices offered by the task icon picker. Shared so the new-task
    /// dialog and the task details dialog offer exactly the same set.
    /// </summary>
    public static class TaskIcons
    {
        public static readonly string[] Choices =
        {
            "", "\uD83D\uDCCB", "\uD83D\uDE80", "\uD83D\uDD25", "\u2B50",
            "\uD83C\uDFAF", "\uD83E\uDDED", "\uD83D\uDCBC", "\uD83C\uDFE0",
            "\uD83D\uDCF1", "\uD83C\uDF93", "\uD83C\uDFE5", "\uD83D\uDE97", "\uD83C\uDF89"
        };
    }
}
