using System.Threading;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository interface for managing PomodoroSettings.
    /// </summary>
    public interface IPomodoroSettingsRepository
    {
        Task<PomodoroSettingsEntity> GetOrCreateAsync(CancellationToken ct = default);
        Task SaveAsync(PomodoroSettingsEntity settings, CancellationToken ct = default);
    }
}
