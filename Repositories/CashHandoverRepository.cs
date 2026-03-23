using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Models.Domain;
using RestaurantMS.Repositories.Interfaces;

namespace RestaurantMS.Repositories;

public class CashHandoverRepository(RestaurantDbContext context)
    : GenericRepository<CashHandover>(context), ICashHandoverRepository
{
    public async Task<CashHandover?> GetOpenHandoverByCashierAsync(int cashierId)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.CashierId == cashierId
                                   && h.Status == HandoverStatus.Open);

    public async Task<IEnumerable<CashHandover>> GetHandoverHistoryAsync(DateTime from, DateTime to)
        => await _dbSet
            .AsNoTracking()
            .Where(h => h.OpenedAt >= from && h.OpenedAt <= to)
            .Include(h => h.Cashier)
            .Include(h => h.Manager)
            .OrderByDescending(h => h.OpenedAt)
            .ToListAsync();

    public async Task<CashHandover?> GetHandoverWithDetailsAsync(int handoverId)
        => await _dbSet
            .AsNoTracking()
            .Include(h => h.Cashier)
            .Include(h => h.Manager)
            .FirstOrDefaultAsync(h => h.Id == handoverId);
}
