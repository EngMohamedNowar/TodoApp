using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository interface for managing FocusSession entities.
    /// </summary>
    public interface IFocusSessionRepository
    {
        Task<List<FocusSession>> GetAllAsync(CancellationToken ct = default);
        Task<List<FocusSession>> GetVisibleAsync(CancellationToken ct = default);
        Task<List<FocusSession>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default);
        Task<int> GetTodayCountAsync(CancellationToken ct = default);
        Task<double> GetTodayHoursAsync(CancellationToken ct = default);
        Task<int> GetTotalDaysAsync(CancellationToken ct = default);
        Task<int> GetStreakAsync(CancellationToken ct = default);
        Task<List<double>> GetWeeklyHoursAsync(DateTime weekStart, CancellationToken ct = default);
        Task AddAsync(FocusSession session, CancellationToken ct = default);
        Task SoftDeleteAllVisibleAsync(CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
