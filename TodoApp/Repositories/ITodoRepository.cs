using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository interface for managing TodoItem entities.
    /// </summary>
    public interface ITodoRepository
    {
        Task<List<TodoItem>> GetAllAsync(CancellationToken ct = default);
        Task<List<TodoItem>> GetFilteredAsync(string? searchText, string? category, bool? isCompleted, CancellationToken ct = default);
        Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddAsync(TodoItem item, CancellationToken ct = default);
        Task UpdateAsync(TodoItem item, CancellationToken ct = default);
        Task DeleteAsync(TodoItem item, CancellationToken ct = default);
        Task DeleteRangeAsync(IEnumerable<TodoItem> items, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<List<string>> GetDistinctCategoriesAsync(CancellationToken ct = default);
    }
}
