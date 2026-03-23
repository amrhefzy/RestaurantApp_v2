using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Models.Domain;
using RestaurantMS.Repositories.Interfaces;

namespace RestaurantMS.Repositories;

public class OrderRepository(RestaurantDbContext context)
    : GenericRepository<PosOrder>(context), IOrderRepository
{
    public async Task<PosOrder?> GetOrderWithItemsAsync(int orderId)
        => await _dbSet
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Payments)
            .Include(o => o.Cashier)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    public async Task<IEnumerable<PosOrder>> GetActiveOrdersAsync()
        => await _dbSet
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.Held)
            .Include(o => o.OrderItems)
            .Include(o => o.Cashier)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<PosOrder>> GetOrdersByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end   = start.AddDays(1);

        return await _dbSet
            .AsNoTracking()
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
            .Include(o => o.OrderItems)
            .Include(o => o.Cashier)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PosOrder>> GetOrdersByShiftAsync(int shiftId)
        => await _dbSet
            .AsNoTracking()
            .Where(o => o.ShiftId == shiftId)
            .Include(o => o.OrderItems)
            .Include(o => o.Cashier)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<string> GetNextOrderNumberAsync()
    {
        var today     = DateTime.UtcNow.Date;
        var tomorrow  = today.AddDays(1);
        var datePart  = today.ToString("yyyyMMdd");

        var todayCount = await _dbSet
            .IgnoreQueryFilters()
            .CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow);

        var sequence = todayCount + 1;
        return $"ORD-{datePart}-{sequence:D4}";
    }
}
