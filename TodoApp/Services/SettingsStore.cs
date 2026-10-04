using System;
using System.IO;
using System.Text.Json;

namespace TodoApp.Services
{
    /// <summary>
    /// Persists small app preferences (currently the accent color) as a JSON
    /// file under %AppData%\TodoApp\settings.json.
    /// </summary>
    public static class SettingsStore
    {
        private static string? _overrideDirectory;

        public static string SettingsDirectory
        {
            get
            {
                if (_overrideDirectory != null) return _overrideDirectory;
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TodoApp");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        /// <summary>Test hook: redirects storage to a temp folder.</summary>
        public static void UseDirectoryForTests(string path) => _overrideDirectory = path;

        private static string FilePath => Path.Combine(SettingsDirectory, "settings.json");

        public sealed class AppPreferences
        {
            public string AccentColor { get; set; } = "#8B7CF6";
        }

        public static AppPreferences Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new AppPreferences();
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppPreferences>(json) ?? new AppPreferences();
            }
            catch
            {
                return new AppPreferences();
            }
        }

        public static void Save(AppPreferences prefs)
        {
            var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
