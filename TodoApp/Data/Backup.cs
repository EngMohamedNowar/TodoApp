using System;
using System.IO;
using System.Linq;

namespace TodoApp.Data
{
    /// <summary>
    /// Creates timestamped copies of todo.db under %AppData%\TodoApp\backups
    /// and prunes older backups, keeping only the most recent ones.
    /// </summary>
    public static class Backup
    {
        private const int MaxBackups = 5;

        public static string BackupDirectory
        {
            get
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TodoApp",
                    "backups");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        /// <summary>
        /// Copies todo.db to a timestamped backup file. Returns the backup path,
        /// or null when no database file exists yet.
        /// </summary>
        public static string? CreateBackupNow()
        {
            var dbPath = TodoDbContext.GetDbPath();
            if (!File.Exists(dbPath)) return null;

            var fileName = $"todo_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var destination = Path.Combine(BackupDirectory, fileName);

            File.Copy(dbPath, destination, overwrite: true);
            PruneOldBackups();

            return destination;
        }

        private static void PruneOldBackups()
        {
            var directory = new DirectoryInfo(BackupDirectory);
            var oldBackups = directory
                .GetFiles("todo_*.db")
                .OrderByDescending(f => f.Name)
                .Skip(MaxBackups);

            foreach (var file in oldBackups)
            {
                try { file.Delete(); } catch { /* best effort */ }
            }
        }
    }
}
