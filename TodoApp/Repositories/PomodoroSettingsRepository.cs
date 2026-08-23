using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository for managing PomodoroSettings via Entity Framework Core.
    /// </summary>
    public class PomodoroSettingsRepository : IPomodoroSettingsRepository
    {
        private readonly TodoDbContext _db;

        public PomodoroSettingsRepository(TodoDbContext db)
        {
            _db = db;
        }

        public async Task<PomodoroSettingsEntity> GetOrCreateAsync(CancellationToken ct = default)
        {
            var settings = await _db.PomodoroSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (settings == null)
            {
                settings = new PomodoroSettingsEntity { Id = 1 };
                _db.PomodoroSettings.Add(settings);
                await _db.SaveChangesAsync(ct);
            }
            return settings;
        }

        public async Task SaveAsync(PomodoroSettingsEntity settings, CancellationToken ct = default)
        {
            var existing = await _db.PomodoroSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (existing == null)
            {
                _db.PomodoroSettings.Add(settings);
            }
            else
            {
                existing.WorkMinutes = settings.WorkMinutes;
                existing.ShortBreakMinutes = settings.ShortBreakMinutes;
                existing.LongBreakMinutes = settings.LongBreakMinutes;
                existing.SessionsBeforeLongBreak = settings.SessionsBeforeLongBreak;
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}
