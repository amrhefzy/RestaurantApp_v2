using RestaurantMS.Models.Domain;

namespace RestaurantMS.Repositories.Interfaces;

public interface IOrderRepository : IGenericRepository<PosOrder>
{
    Task<PosOrder?> GetOrderWithItemsAsync(int orderId);
    Task<IEnumerable<PosOrder>> GetActiveOrdersAsync();
    Task<IEnumerable<PosOrder>> GetOrdersByDateAsync(DateTime date);
    Task<IEnumerable<PosOrder>> GetOrdersByShiftAsync(int shiftId);
    Task<string> GetNextOrderNumberAsync();
}
