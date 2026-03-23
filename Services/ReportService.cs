using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Helpers;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Repositories.Interfaces;
using RestaurantMS.Services.Interfaces;
using System.Text;

namespace RestaurantMS.Services;

public class ReportService(IUnitOfWork uow, RestaurantDbContext context) : IReportService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly RestaurantDbContext _context = context;

    // ── Daily Sales Report ────────────────────────────────────────────────────
    public async Task<ApiResponse<DailySalesReportDto>> GetDailySalesReportAsync(DateTime date)
    {
        var orders = (await _uow.Orders.GetOrdersByDateAsync(date)).ToList();
        return ApiResponse<DailySalesReportDto>.Ok(BuildDailyReport(date, orders));
    }

    // ── Best Selling Items (date range) ───────────────────────────────────────
    public async Task<ApiResponse<List<BestSellingItemDto>>> GetBestSellingItemsAsync(
        DateTime from, DateTime to, int top = 10)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1);

        var items = await _context.PosOrderItems
            .AsNoTracking()
            .Include(i => i.MenuItem)
            .Include(i => i.Order)
            .Where(i => !i.IsDeleted
                        && i.Order != null
                        && i.Order.Status == OrderStatus.Paid
                        && i.Order.PaidAt >= start
                        && i.Order.PaidAt < end)
            .ToListAsync();

        var result = items
            .GroupBy(i => i.MenuItemId)
            .Select(g => new BestSellingItemDto
            {
                MenuItemId    = g.Key,
                NameAr        = g.First().MenuItem?.NameAr ?? string.Empty,
                NameEn        = g.First().MenuItem?.NameEn ?? string.Empty,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue  = g.Sum(i => i.TotalPrice),
                OrderCount    = g.Select(i => i.OrderId).Distinct().Count(),
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(top)
            .ToList();

        return ApiResponse<List<BestSellingItemDto>>.Ok(result);
    }

    // ── Z-Report (end-of-day) ─────────────────────────────────────────────────
    public async Task<ApiResponse<ZReportDto>> GetZReportAsync(DateTime date)
    {
        var orders = (await _uow.Orders.GetOrdersByDateAsync(date)).ToList();
        var daily  = BuildDailyReport(date, orders);

        var report = new ZReportDto
        {
            Date           = daily.Date,
            TotalOrders    = daily.TotalOrders,
            SubTotal       = daily.SubTotal,
            TaxAmount      = daily.TaxAmount,
            DiscountAmount = daily.DiscountAmount,
            NetSales       = daily.NetSales,
            CashTotal      = daily.CashTotal,
            CardTotal      = daily.CardTotal,
            VoidedOrders   = daily.VoidedOrders,
            TopItems       = daily.TopItems,
            HourlySales    = daily.HourlySales,
            GeneratedAt    = DateTime.UtcNow,
            IsClosed       = true,
        };

        return ApiResponse<ZReportDto>.Ok(report);
    }

    // ── X-Report (current shift / today) ─────────────────────────────────────
    public async Task<ApiResponse<XReportDto>> GetXReportAsync(int? shiftId = null)
    {
        var orders = shiftId.HasValue
            ? (await _uow.Orders.GetOrdersByShiftAsync(shiftId.Value)).ToList()
            : (await _uow.Orders.GetOrdersByDateAsync(DateTime.UtcNow.Date)).ToList();

        var paid        = orders.Where(o => o.Status == OrderStatus.Paid).ToList();
        var allPayments = paid.SelectMany(o => o.Payments).ToList();

        var report = new XReportDto
        {
            ShiftId     = shiftId,
            From        = orders.Any() ? orders.Min(o => o.CreatedAt) : DateTime.UtcNow,
            To          = DateTime.UtcNow,
            TotalOrders = paid.Count,
            SubTotal    = paid.Sum(o => o.SubTotal),
            TaxAmount   = paid.Sum(o => o.TaxAmount),
            NetSales    = paid.Sum(o => o.TotalAmount),
            CashTotal   = allPayments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount),
            CardTotal   = allPayments.Where(p => p.PaymentMethod == PaymentMethod.Card).Sum(p => p.Amount),
            HourlySales = paid
                .GroupBy(o => o.PaidAt.HasValue ? o.PaidAt.Value.Hour : o.CreatedAt.Hour)
                .Select(g => new HourlySalesDto
                {
                    Hour        = g.Key,
                    OrderCount  = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                })
                .OrderBy(h => h.Hour)
                .ToList(),
        };

        return ApiResponse<XReportDto>.Ok(report);
    }

    // ── Hourly Sales ──────────────────────────────────────────────────────────
    public async Task<ApiResponse<List<HourlySalesDto>>> GetHourlySalesAsync(DateTime date)
    {
        var hourly = (await _uow.Orders.GetOrdersByDateAsync(date))
            .Where(o => o.Status == OrderStatus.Paid)
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

    // ── Export CSV (no Excel package — returns UTF-8 CSV) ─────────────────────
    public async Task<ApiResponse<byte[]>> ExportDailyReportExcelAsync(DateTime date)
    {
        var orders = (await _uow.Orders.GetOrdersByDateAsync(date))
            .Where(o => o.Status == OrderStatus.Paid)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Date,OrderNumber,SubTotal,TaxAmount,DiscountAmount,TotalAmount,PaymentMethod");

        foreach (var o in orders)
        {
            var method = o.Payments.FirstOrDefault()?.PaymentMethod.ToString() ?? "N/A";
            var ts     = o.PaidAt?.ToString("yyyy-MM-dd HH:mm") ?? o.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            sb.AppendLine(
                $"{ts},{o.OrderNumber},{o.SubTotal:F3},{o.TaxAmount:F3},{o.DiscountAmount:F3},{o.TotalAmount:F3},{method}");
        }

        return ApiResponse<byte[]>.Ok(Encoding.UTF8.GetBytes(sb.ToString()), "Report exported.");
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private static DailySalesReportDto BuildDailyReport(DateTime date, List<PosOrder> orders)
    {
        var paid        = orders.Where(o => o.Status == OrderStatus.Paid).ToList();
        var voided      = orders.Count(o => o.Status == OrderStatus.Voided);
        var allPayments = paid.SelectMany(o => o.Payments).ToList();

        var topItems = paid
            .SelectMany(o => o.OrderItems)
            .Where(i => !i.IsDeleted)
            .GroupBy(i => i.MenuItemId)
            .Select(g => new BestSellingItemDto
            {
                MenuItemId    = g.Key,
                NameAr        = g.First().MenuItem?.NameAr ?? string.Empty,
                NameEn        = g.First().MenuItem?.NameEn ?? string.Empty,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue  = g.Sum(i => i.TotalPrice),
                OrderCount    = g.Select(i => i.OrderId).Distinct().Count(),
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(10)
            .ToList();

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

        return new DailySalesReportDto
        {
            Date           = date.Date,
            TotalOrders    = paid.Count,
            SubTotal       = paid.Sum(o => o.SubTotal),
            TaxAmount      = paid.Sum(o => o.TaxAmount),
            DiscountAmount = paid.Sum(o => o.DiscountAmount),
            NetSales       = paid.Sum(o => o.TotalAmount),
            CashTotal      = allPayments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount),
            CardTotal      = allPayments.Where(p => p.PaymentMethod == PaymentMethod.Card).Sum(p => p.Amount),
            VoidedOrders   = voided,
            TopItems       = topItems,
            HourlySales    = hourly,
        };
    }
}
