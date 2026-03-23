using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Models.Domain;
using RestaurantMS.Repositories.Interfaces;

namespace RestaurantMS.Repositories;

public class ShiftRepository(RestaurantDbContext context)
    : GenericRepository<EmployeeShift>(context), IShiftRepository
{
    public async Task<IEnumerable<EmployeeShift>> GetShiftsByDateRangeAsync(DateTime from, DateTime to)
        => await _dbSet
            .AsNoTracking()
            .Where(s => s.ShiftDate >= from && s.ShiftDate <= to)
            .Include(s => s.Employee)
            .Include(s => s.Template)
            .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.PlannedStart)
            .ToListAsync();

    public async Task<EmployeeShift?> GetActiveShiftByEmployeeAsync(int employeeId)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId
                                   && s.Status == ShiftStatus.Active);

    public async Task<EmployeeShift?> GetShiftWithTemplateAsync(int shiftId)
        => await _dbSet
            .AsNoTracking()
            .Include(s => s.Template)
            .Include(s => s.Employee)
            .Include(s => s.Manager)
            .FirstOrDefaultAsync(s => s.Id == shiftId);
}
