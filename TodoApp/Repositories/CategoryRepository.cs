using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Repositories
{
    /// <summary>
    /// Repository for managing Category entities.
    /// </summary>
    public class CategoryRepository : ICategoryRepository
    {
        private readonly TodoDbContext _db;

        public CategoryRepository(TodoDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Category category, CancellationToken ct = default)
        {
            if (!await ExistsAsync(category.Name, ct))
            {
                await _db.Categories.AddAsync(category, ct);
            }
        }

        public async Task DeleteByNameAsync(string name, CancellationToken ct = default)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Name == name, ct);

            if (category != null)
            {
                _db.Categories.Remove(category);
            }
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken ct = default)
        {
            return await _db.Categories.AnyAsync(c => c.Name == name, ct);
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _db.SaveChangesAsync(ct);
        }
    }
}
