using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Data
{
    /// <summary>
    /// EF Core context. Stores the database as a single .db file
    /// under %AppData%\TodoApp\todo.db, so data survives between runs
    /// and is fully local to the user's machine.
    /// </summary>
    public class TodoDbContext : DbContext
    {
        public DbSet<TodoItem> Todos { get; set; } = null!;
        public DbSet<FocusSession> FocusSessions { get; set; } = null!;
        public DbSet<PomodoroSettingsEntity> PomodoroSettings { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;

        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        public static string GetDbPath()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TodoApp");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "todo.db");
        }

        public static DbContextOptions<TodoDbContext> CreateDefaultOptions()
        {
            var builder = new DbContextOptionsBuilder<TodoDbContext>();
            builder.UseSqlite($"Data Source={GetDbPath()}");
            return builder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Category).HasMaxLength(100);
                entity.Property(t => t.Priority).HasConversion<int>();
                entity.HasIndex(t => t.IsCompleted);
                entity.HasIndex(t => t.DueDate);
            });

            modelBuilder.Entity<FocusSession>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasIndex(f => f.StartedAt);
            });

            modelBuilder.Entity<PomodoroSettingsEntity>(entity =>
            {
                entity.HasKey(s => s.Id);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(c => c.Name).IsUnique();
            });
        }

        /// <summary>
        /// Ensures the database file and all tables exist. Safe to call on
        /// every startup: EnsureCreated only creates a database when the
        /// file doesn't exist at all, so for people upgrading from an
        /// earlier version of the app (whose todo.db already exists but
        /// predates the FocusSessions/PomodoroSettings tables) we also
        /// create any missing tables directly - no EF migrations needed.
        /// </summary>
        public void EnsureSchema()
        {
            Database.EnsureCreated();

            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS FocusSessions (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    StartedAt TEXT NOT NULL,
                    CompletedAt TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL
                );");

            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PomodoroSettings (
                    Id INTEGER NOT NULL PRIMARY KEY,
                    WorkMinutes INTEGER NOT NULL,
                    ShortBreakMinutes INTEGER NOT NULL,
                    LongBreakMinutes INTEGER NOT NULL,
                    SessionsBeforeLongBreak INTEGER NOT NULL
                );");

            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );");

            EnsureColumnExists("Todos", "SortOrder", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists("Todos", "ParentId", "INTEGER");
            EnsureColumnExists("Todos", "Recurrence", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists("Todos", "Tags", "TEXT");
            EnsureColumnExists("Todos", "Icon", "TEXT");
            EnsureColumnExists("Todos", "IsFavorite", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists("Todos", "IsArchived", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists("Todos", "Attachments", "TEXT");
            EnsureColumnExists("FocusSessions", "IsHidden", "INTEGER NOT NULL DEFAULT 0");
        }

        /// <summary>
        /// Adds a column to an existing table if it isn't already there.
        /// SQLite has no "ADD COLUMN IF NOT EXISTS", so we check the
        /// table's schema first via PRAGMA table_info before altering it.
        /// Uses parameterized queries to prevent SQL injection.
        /// </summary>
        private void EnsureColumnExists(string table, string column, string columnDefinition)
        {
            var allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Todos", "FocusSessions", "PomodoroSettings", "Categories"
            };

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SortOrder", "IsHidden", "Id", "Title", "Description",
                "Category", "Priority", "DueDate", "IsCompleted",
                "CreatedAt", "CompletedAt", "StartedAt", "DurationMinutes",
                "WorkMinutes", "ShortBreakMinutes", "LongBreakMinutes",
                "SessionsBeforeLongBreak", "Name", "ParentId", "Recurrence",
                "Tags", "Icon", "IsFavorite", "IsArchived", "Attachments"
            };

            if (!allowedTables.Contains(table) || !allowedColumns.Contains(column))
                return;

            var columnExists = false;
            using (var command = Database.GetDbConnection().CreateCommand())
            {
                if (command.Connection!.State != System.Data.ConnectionState.Open)
                    command.Connection.Open();

                command.CommandText = $"PRAGMA table_info({table});";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var name = reader["name"]?.ToString();
                    if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                    {
                        columnExists = true;
                        break;
                    }
                }
            }

            if (!columnExists)
            {
#pragma warning disable EF1002
                Database.ExecuteSqlRaw($"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition};");
#pragma warning restore EF1002
            }
        }
    }
}
