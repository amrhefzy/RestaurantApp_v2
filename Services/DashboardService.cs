using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Helpers;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Repositories.Interfaces;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Services;

public class DashboardService(IUnitOfWork uow, RestaurantDbContext context) : IDashboardService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly RestaurantDbContext _context = context;

    private static readonly OrderStatus[] ActiveStatuses =
    [
        OrderStatus.Open, OrderStatus.Held,
        OrderStatus.SentToKitchen, OrderStatus.Preparing,
        OrderStatus.Ready, OrderStatus.Served
    ];

    public async Task<ApiResponse<DashboardDto>> GetDashboardDataAsync()
    {
        var today       = DateTime.UtcNow.Date;
        var todayOrders = (await _uow.Orders.GetOrdersByDateAsync(today)).ToList();

        var paid    = todayOrders.Where(o => o.Status is OrderStatus.Paid or OrderStatus.Closed).ToList();
        var active  = todayOrders.Where(o => ActiveStatuses.Contains(o.Status)).ToList();
        var voided  = todayOrders.Count(o => o.Status is OrderStatus.Voided or OrderStatus.Cancelled);

        var todaySales = paid.Sum(o => o.TotalAmount);
        var avgOrder   = paid.Any() ? paid.Average(o => o.TotalAmount) : 0m;

        // Active shifts
        var activeShifts = await _uow.Shifts.CountAsync(
            s => s.ShiftDate == today && (s.Status == ShiftStatus.Active || s.Status == ShiftStatus.ClosingPending));

        // Pending / flagged handovers
        var flagged = await _uow.CashHandovers.CountAsync(
            h => h.Status == HandoverStatus.Flagged || h.Status == HandoverStatus.PendingApproval);

        var flaggedSum = (await _context.CashHandovers
            .AsNoTracking()
            .Where(h => !h.IsDeleted && (h.Status == HandoverStatus.Flagged || h.Status == HandoverStatus.PendingApproval) && h.Difference.HasValue)
            .ToListAsync())
            .Sum(h => Math.Abs(h.Difference!.Value));

        // Best sellers last 7 days
        var weekAgo  = today.AddDays(-7);
        var weekEnd  = today.AddDays(1);
        var bestSellers = await _context.PosOrderItems
            .AsNoTracking()
            .Include(i => i.MenuItem)
            .Include(i => i.Order)
            .Where(i => !i.IsDeleted
                        && i.Order != null
                        && i.Order.Status == OrderStatus.Paid
                        && i.Order.PaidAt >= weekAgo
                        && i.Order.PaidAt < weekEnd)
            .GroupBy(i => i.MenuItemId)
            .Select(g => new BestSellingItemDto
            {
                MenuItemId    = g.Key,
                NameAr        = g.First().MenuItem!.NameAr,
                NameEn        = g.First().MenuItem!.NameEn,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue  = g.Sum(i => i.TotalPrice),
                OrderCount    = g.Select(i => i.OrderId).Distinct().Count(),
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(7)
            .ToListAsync();

        // Hourly sales today
        var hourly = paid
            .GroupBy(o => o.PaidAt.HasValue ? o.PaidAt.Value.Hour : o.CreatedAt.Hour)
            .Select(g => new HourlySalesDto
            {
                Hour        = g.Key,
                OrderCount  = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
            })
            .OrderBy(h => h.Hour)
            .ToList();

        // Active orders summary
        var activeList = active.Select(o => MapActive(o)).ToList();

        return ApiResponse<DashboardDto>.Ok(new DashboardDto
        {
            TodaySales        = todaySales,
            TodayOrders       = paid.Count,
            ActiveOrders      = active.Count,
            ActiveShifts      = activeShifts,
            AvgOrderValue     = avgOrder,
            PendingDifference = flaggedSum,
            VoidedToday       = voided,
            PendingHandovers  = flagged,
            HourlySales       = hourly,
            BestSellers       = bestSellers,
            ActiveOrdersList  = activeList,
        });
    }

    public async Task<ApiResponse<List<ActiveOrderSummaryDto>>> GetActiveOrdersAsync()
    {
        var today   = DateTime.UtcNow.Date;
        var orders  = (await _uow.Orders.GetOrdersByDateAsync(today))
            .Where(o => ActiveStatuses.Contains(o.Status))
            .ToList();

        return ApiResponse<List<ActiveOrderSummaryDto>>.Ok(orders.Select(MapActive).ToList());
    }

    public async Task<ApiResponse<List<HourlySalesDto>>> GetTodayHourlySalesAsync()
    {
        var today  = DateTime.UtcNow.Date;
        var orders = (await _uow.Orders.GetOrdersByDateAsync(today))
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Closed)
            .ToList();

        var hourly = orders
            .GroupBy(o => o.PaidAt.HasValue ? o.PaidAt.Value.Hour : o.CreatedAt.Hour)
            .Select(g => new HourlySalesDto
            {
                Hour        = g.Key,
                OrderCount  = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
            })
            .OrderBy(h => h.Hour)
            .ToList();

        return ApiResponse<List<HourlySalesDto>>.Ok(hourly);
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private static ActiveOrderSummaryDto MapActive(PosOrder o)
    {
        var elapsed = (int)(DateTime.UtcNow - o.CreatedAt).TotalMinutes;
        var (label, cls) = o.Status switch
        {
            OrderStatus.Open          => ("Open", "success"),
            OrderStatus.Held          => ("On Hold", "warning"),
            OrderStatus.SentToKitchen => ("Sent to Kitchen", "info"),
            OrderStatus.Preparing     => ("Preparing", "primary"),
            OrderStatus.Ready         => ("Ready", "success"),
            OrderStatus.Served        => ("Served", "secondary"),
            _                         => (o.Status.ToString(), "light"),
        };

        return new ActiveOrderSummaryDto
        {
            Id          = o.Id,
            OrderNumber = o.OrderNumber,
            OrderType   = o.OrderType.ToString(),
            Status      = label,
            StatusClass = cls,
            Total       = o.TotalAmount,
            ItemCount   = o.OrderItems?.Count(i => !i.IsDeleted) ?? 0,
            TableNumber = o.TableNumber ?? "—",
            CreatedAt   = o.CreatedAt,
            ElapsedMin  = elapsed,
        };
    }
}
