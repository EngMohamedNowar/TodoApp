using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository for managing TodoItem entities via Entity Framework Core.
    /// </summary>
    public class TodoRepository : ITodoRepository
    {
        private readonly TodoDbContext _db;

        public TodoRepository(TodoDbContext db)
        {
            _db = db;
        }

        public async Task<List<TodoItem>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Todos
                .OrderBy(t => t.SortOrder)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<TodoItem>> GetFilteredAsync(
            string? searchText, string? category, bool? isCompleted, CancellationToken ct = default)
        {
            var query = _db.Todos.AsQueryable();

            if (isCompleted.HasValue)
                query = query.Where(t => t.IsCompleted == isCompleted.Value);

            if (!string.IsNullOrWhiteSpace(category) && category != "All Categories")
            {
                var cat = category == "Uncategorized" ? null : category;
                query = cat == null
                    ? query.Where(t => t.Category == null || t.Category == "")
                    : query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var term = searchText.Trim();
                query = query.Where(t =>
                    (t.Title != null && t.Title.Contains(term)) ||
                    (t.Description != null && t.Description.Contains(term)));
            }

            return await query
                .OrderBy(t => t.SortOrder)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Todos.FindAsync(new object[] { id }, ct);
        }

        public async Task AddAsync(TodoItem item, CancellationToken ct = default)
        {
            await _db.Todos.AddAsync(item, ct);
        }

        public Task UpdateAsync(TodoItem item, CancellationToken ct = default)
        {
            _db.Todos.Update(item);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TodoItem item, CancellationToken ct = default)
        {
            _db.Todos.Remove(item);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<TodoItem> items, CancellationToken ct = default)
        {
            _db.Todos.RemoveRange(items);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _db.SaveChangesAsync(ct);
        }

        public async Task<List<string>> GetDistinctCategoriesAsync(CancellationToken ct = default)
        {
            return await _db.Todos
                .Where(t => !string.IsNullOrWhiteSpace(t.Category))
                .Select(t => t.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct);
        }
    }
}
