using RestaurantMS.Models.Domain;

namespace RestaurantMS.Repositories.Interfaces;

public interface IShiftRepository : IGenericRepository<EmployeeShift>
{
    Task<IEnumerable<EmployeeShift>> GetShiftsByDateRangeAsync(DateTime from, DateTime to);
    Task<EmployeeShift?> GetActiveShiftByEmployeeAsync(int employeeId);
    Task<EmployeeShift?> GetShiftWithTemplateAsync(int shiftId);
}
