using Microsoft.EntityFrameworkCore.Storage;
using RestaurantMS.Data;
using RestaurantMS.Models.Domain;
using RestaurantMS.Repositories.Interfaces;

namespace RestaurantMS.Repositories;

public class UnitOfWork(RestaurantDbContext context) : IUnitOfWork
{
    private readonly RestaurantDbContext _context = context;
    private IDbContextTransaction? _transaction;

    // ── Lazy-initialized repositories ────────────────────────────────────────
    private IOrderRepository?                   _orders;
    private IMenuRepository?                    _menuItems;
    private IGenericRepository<Category>?       _categories;
    private ICashHandoverRepository?            _cashHandovers;
    private IShiftRepository?                   _shifts;
    private IGenericRepository<ShiftTemplate>?  _shiftTemplates;
    private IGenericRepository<PrinterProfile>? _printerProfiles;
    private IGenericRepository<Payment>?        _payments;
    private IGenericRepository<PosOrderItem>?   _orderItems;

    public IOrderRepository Orders
        => _orders ??= new OrderRepository(_context);

    public IMenuRepository MenuItems
        => _menuItems ??= new MenuRepository(_context);

    public IGenericRepository<Category> Categories
        => _categories ??= new GenericRepository<Category>(_context);

    public ICashHandoverRepository CashHandovers
        => _cashHandovers ??= new CashHandoverRepository(_context);

    public IShiftRepository Shifts
        => _shifts ??= new ShiftRepository(_context);

    public IGenericRepository<ShiftTemplate> ShiftTemplates
        => _shiftTemplates ??= new GenericRepository<ShiftTemplate>(_context);

    public IGenericRepository<PrinterProfile> PrinterProfiles
        => _printerProfiles ??= new GenericRepository<PrinterProfile>(_context);

    public IGenericRepository<Payment> Payments
        => _payments ??= new GenericRepository<Payment>(_context);

    public IGenericRepository<PosOrderItem> OrderItems
        => _orderItems ??= new GenericRepository<PosOrderItem>(_context);

    // ── Persistence ──────────────────────────────────────────────────────────
    public async Task<int> CompleteAsync()
        => await _context.SaveChangesAsync();

    // ── Transaction management ───────────────────────────────────────────────
    public async Task BeginTransactionAsync()
        => _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null) return;

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    // ── Dispose ──────────────────────────────────────────────────────────────
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
