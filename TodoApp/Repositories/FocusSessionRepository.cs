using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository for managing FocusSession entities via Entity Framework Core.
    /// </summary>
    public class FocusSessionRepository : IFocusSessionRepository
    {
        private readonly TodoDbContext _db;

        public FocusSessionRepository(TodoDbContext db)
        {
            _db = db;
        }

        public async Task<List<FocusSession>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.FocusSessions
                .OrderByDescending(f => f.StartedAt)
                .ToListAsync(ct);
        }

        public async Task<List<FocusSession>> GetVisibleAsync(CancellationToken ct = default)
        {
            return await _db.FocusSessions
                .Where(f => !f.IsHidden)
                .OrderByDescending(f => f.StartedAt)
                .Take(200)
                .ToListAsync(ct);
        }

        public async Task<List<FocusSession>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default)
        {
            return await _db.FocusSessions
                .Where(f => f.StartedAt >= start && f.StartedAt < end)
                .ToListAsync(ct);
        }

        public async Task<int> GetTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.Today;
            return await _db.FocusSessions
                .CountAsync(f => f.StartedAt >= today && f.StartedAt < today.AddDays(1), ct);
        }

        public async Task<double> GetTodayHoursAsync(CancellationToken ct = default)
        {
            var today = DateTime.Today;
            var minutes = await _db.FocusSessions
                .Where(f => f.StartedAt >= today && f.StartedAt < today.AddDays(1))
                .SumAsync(f => (int?)f.DurationMinutes, ct) ?? 0;
            return Math.Round(minutes / 60.0, 1);
        }

        public async Task<int> GetTotalDaysAsync(CancellationToken ct = default)
        {
            return await _db.FocusSessions
                .Select(f => f.StartedAt.Date)
                .Distinct()
                .CountAsync(ct);
        }

        public async Task<int> GetStreakAsync(CancellationToken ct = default)
        {
            var today = DateTime.Today;
            var distinctDates = await _db.FocusSessions
                .Select(f => f.StartedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync(ct);

            if (!distinctDates.Contains(today))
            {
                if (!distinctDates.Contains(today.AddDays(-1)))
                    return 0;

                var cursor = today.AddDays(-1);
                var streak = 0;
                while (distinctDates.Contains(cursor))
                {
                    streak++;
                    cursor = cursor.AddDays(-1);
                }
                return streak;
            }

            var streakCount = 0;
            var date = today;
            while (distinctDates.Contains(date))
            {
                streakCount++;
                date = date.AddDays(-1);
            }
            return streakCount;
        }

        public async Task<List<double>> GetWeeklyHoursAsync(DateTime weekStart, CancellationToken ct = default)
        {
            var dailyHours = new List<double>();
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var minutes = await _db.FocusSessions
                    .Where(f => f.StartedAt >= day && f.StartedAt < day.AddDays(1))
                    .SumAsync(f => (int?)f.DurationMinutes, ct) ?? 0;
                dailyHours.Add(minutes / 60.0);
            }
            return dailyHours;
        }

        public async Task AddAsync(FocusSession session, CancellationToken ct = default)
        {
            await _db.FocusSessions.AddAsync(session, ct);
        }

        public async Task SoftDeleteAllVisibleAsync(CancellationToken ct = default)
        {
            var visibleSessions = await _db.FocusSessions
                .Where(f => !f.IsHidden)
                .ToListAsync(ct);

            foreach (var s in visibleSessions)
            {
                s.IsHidden = true;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _db.SaveChangesAsync(ct);
        }
    }
}
