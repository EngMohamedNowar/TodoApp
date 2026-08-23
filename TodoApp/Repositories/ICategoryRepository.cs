using System.Threading;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository interface for managing Category entities.
    /// </summary>
    public interface ICategoryRepository
    {
        Task AddAsync(Category category, CancellationToken ct = default);
        Task DeleteByNameAsync(string name, CancellationToken ct = default);
        Task<bool> ExistsAsync(string name, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
