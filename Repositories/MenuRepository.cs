using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Models.Domain;
using RestaurantMS.Repositories.Interfaces;

namespace RestaurantMS.Repositories;

public class MenuRepository(RestaurantDbContext context)
    : GenericRepository<MenuItem>(context), IMenuRepository
{
    public async Task<IEnumerable<Category>> GetMenuWithCategoriesAsync()
        => await context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Include(c => c.MenuItems
                .Where(m => !m.IsDeleted && m.IsAvailable)
                .OrderBy(m => m.DisplayOrder))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

    public async Task<MenuItem?> GetByBarcodeAsync(string barcode)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Barcode == barcode);

    public async Task<IEnumerable<MenuItem>> SearchMenuAsync(string term)
    {
        var lower = term.ToLower();
        return await _dbSet
            .AsNoTracking()
            .Include(m => m.Category)
            .Where(m => m.NameAr.ToLower().Contains(lower)
                     || m.NameEn.ToLower().Contains(lower))
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }
}
