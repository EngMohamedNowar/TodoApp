using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Data;
using TodoApp.Repositories;
using TodoApp.ViewModels;
using TodoApp.Views;

namespace TodoApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddDbContextFactory<TodoDbContext>(options =>
                options.UseSqlite($"Data Source={TodoDbContext.GetDbPath()}"));

            services.AddScoped<TodoDbContext>(sp =>
            {
                var options = TodoDbContext.CreateDefaultOptions();
                var db = new TodoDbContext(options);
                db.EnsureSchema();
                return db;
            });

            services.AddScoped<ITodoRepository, TodoRepository>();
            services.AddScoped<IFocusSessionRepository, FocusSessionRepository>();
            services.AddScoped<IPomodoroSettingsRepository, PomodoroSettingsRepository>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<PomodoroViewModel>();
            services.AddTransient<PomodoroSettingsViewModel>();
            services.AddTransient<FocusStatsViewModel>();
            services.AddTransient<AddEditTodoViewModel>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services = BuildServiceProvider();

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            ApplySavedTheme();
            TryAutoBackup();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }

        private static void ApplySavedTheme()
        {
            try
            {
                var prefs = TodoApp.Services.SettingsStore.Load();
                TodoApp.Services.ThemeService.ApplyAccent(prefs.AccentColor, persist: false);
            }
            catch
            {
                // theme is cosmetic; never block startup
            }
        }

        private static void TryAutoBackup()
        {
            try
            {
                Data.Backup.CreateBackupNow();
            }
            catch
            {
                // backup is best-effort; never block startup on it
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}
