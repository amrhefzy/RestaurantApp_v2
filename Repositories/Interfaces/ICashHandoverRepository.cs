using RestaurantMS.Models.Domain;

namespace RestaurantMS.Repositories.Interfaces;

public interface ICashHandoverRepository : IGenericRepository<CashHandover>
{
    Task<CashHandover?> GetOpenHandoverByCashierAsync(int cashierId);
    Task<IEnumerable<CashHandover>> GetHandoverHistoryAsync(DateTime from, DateTime to);
    Task<CashHandover?> GetHandoverWithDetailsAsync(int handoverId);
}
