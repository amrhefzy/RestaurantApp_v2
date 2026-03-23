using RestaurantMS.Models.Domain;

namespace RestaurantMS.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IOrderRepository           Orders         { get; }
    IMenuRepository            MenuItems      { get; }
    IGenericRepository<Category>      Categories     { get; }
    ICashHandoverRepository    CashHandovers  { get; }
    IShiftRepository           Shifts         { get; }
    IGenericRepository<ShiftTemplate>  ShiftTemplates { get; }
    IGenericRepository<PrinterProfile> PrinterProfiles { get; }
    IGenericRepository<Payment>        Payments       { get; }

    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
