using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using RestaurantMS.Helpers;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Repositories.Interfaces;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Services;

/// <summary>
/// Requires NuGet: BCrypt.Net-Next
/// </summary>
public class HandoverService(
    IUnitOfWork uow,
    IConfiguration config,
    UserManager<ApplicationUser> userManager) : IHandoverService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // ── Open Handover ─────────────────────────────────────────────────────────
    public async Task<ApiResponse<CashHandoverDto>> OpenHandoverAsync(OpenHandoverDto dto, int cashierId)
    {
        var existing = await _uow.CashHandovers.GetOpenHandoverByCashierAsync(cashierId);
        if (existing is not null)
            return ApiResponse<CashHandoverDto>.Fail(
                "A handover is already open for this cashier. Close it before opening a new one.");

        var handover = new CashHandover
        {
            CashierId      = cashierId,
            OpenFloat      = dto.OpenFloat,
            CashSalesTotal = 0,
            CashRefundsTotal = 0,
            Status         = HandoverStatus.Open,
            OpenedAt       = DateTime.UtcNow,
        };

        await _uow.CashHandovers.AddAsync(handover);
        await _uow.CompleteAsync();

        var saved = await _uow.CashHandovers.GetHandoverWithDetailsAsync(handover.Id);
        return ApiResponse<CashHandoverDto>.Ok(MapHandover(saved!), "Handover opened.");
    }

    // ── Get Current ───────────────────────────────────────────────────────────
    public async Task<ApiResponse<CashHandoverDto>> GetCurrentHandoverAsync(int cashierId)
    {
        var handover = await _uow.CashHandovers.GetOpenHandoverByCashierAsync(cashierId);
        if (handover is null)
            return ApiResponse<CashHandoverDto>.Fail("No open handover found for this cashier.", 404);

        var full = await _uow.CashHandovers.GetHandoverWithDetailsAsync(handover.Id);
        return ApiResponse<CashHandoverDto>.Ok(MapHandover(full!));
    }

    // ── Get Summary ───────────────────────────────────────────────────────────
    public async Task<ApiResponse<CashHandoverSummaryDto>> GetHandoverSummaryAsync(int handoverId)
    {
        var handover = await _uow.CashHandovers.GetHandoverWithDetailsAsync(handoverId);
        if (handover is null)
            return ApiResponse<CashHandoverSummaryDto>.Fail("Handover not found.", 404);

        // Count orders paid during this handover period
        var periodEnd   = handover.ClosedAt ?? DateTime.UtcNow;
        var paidOrders  = await _uow.Orders.GetOrdersByDateAsync(handover.OpenedAt.Date);
        var cashierOrders = paidOrders.Where(o =>
            o.CashierId == handover.CashierId &&
            o.Status    == OrderStatus.Paid   &&
            o.PaidAt    >= handover.OpenedAt  &&
            o.PaidAt    <= periodEnd).ToList();

        var cardTotal = cashierOrders
            .SelectMany(o => o.Payments)
            .Where(p => p.PaymentMethod == PaymentMethod.Card)
            .Sum(p => p.Amount);

        return ApiResponse<CashHandoverSummaryDto>.Ok(new CashHandoverSummaryDto
        {
            Id               = handover.Id,
            CashierName      = handover.Cashier is not null
                ? $"{handover.Cashier.FullNameAr} / {handover.Cashier.FullNameEn}"
                : string.Empty,
            OpenFloat        = handover.OpenFloat,
            CashSalesTotal   = handover.CashSalesTotal,
            CashRefundsTotal = handover.CashRefundsTotal,
            ExpectedCash     = handover.ExpectedCash,
            ActualCash       = handover.ActualCash,
            Difference       = handover.Difference,
            Status           = handover.Status,
            OpenedAt         = handover.OpenedAt,
            ClosedAt         = handover.ClosedAt,
            TotalOrders      = cashierOrders.Count,
            TotalCardSales   = cardTotal,
        });
    }

    // ── Close Handover ────────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> CloseHandoverAsync(CloseHandoverDto dto, int cashierId)
    {
        var handover = await _uow.CashHandovers.GetByIdTrackedAsync(dto.HandoverId);
        if (handover is null)
            return ApiResponse<bool>.Fail("Handover not found.", 404);
        if (handover.CashierId != cashierId)
            return ApiResponse<bool>.Fail("You can only close your own handover.", 403);
        if (handover.Status != HandoverStatus.Open)
            return ApiResponse<bool>.Fail("Handover is not open.");

        var threshold  = _config.GetValue<decimal>("PosSettings:HandoverDifferenceThreshold", 5m);
        var difference = dto.ActualCash - handover.ExpectedCash;

        handover.ActualCash    = dto.ActualCash;
        handover.Difference    = difference;
        handover.ClosedAt      = DateTime.UtcNow;
        handover.CashierNotes  = dto.CashierNotes;
        handover.Status        = Math.Abs(difference) > threshold
            ? HandoverStatus.Flagged
            : HandoverStatus.PendingApproval;

        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true,
            handover.Status == HandoverStatus.Flagged
                ? $"Handover closed with a difference of {difference:F3}. Flagged for manager review."
                : "Handover closed. Pending manager approval.");
    }

    // ── Approve Handover ──────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> ApproveHandoverAsync(
        int handoverId, string managerPin, int managerId, string? notes)
    {
        if (!await VerifyManagerPinAsync(managerPin))
            return ApiResponse<bool>.Fail("Invalid Manager PIN.");

        var handover = await _uow.CashHandovers.GetByIdTrackedAsync(handoverId);
        if (handover is null)
            return ApiResponse<bool>.Fail("Handover not found.", 404);
        if (handover.Status is not (HandoverStatus.PendingApproval or HandoverStatus.Flagged))
            return ApiResponse<bool>.Fail("Handover is not pending approval.");

        handover.Status              = HandoverStatus.Closed;
        handover.ManagerId           = managerId;
        handover.ManagerApprovedAt   = DateTime.UtcNow;
        handover.ManagerNotes        = notes;

        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true, "Handover approved and closed.");
    }

    // ── History ───────────────────────────────────────────────────────────────
    public async Task<ApiResponse<List<CashHandoverDto>>> GetHandoverHistoryAsync(DateTime from, DateTime to)
    {
        var list = await _uow.CashHandovers.GetHandoverHistoryAsync(from, to);
        return ApiResponse<List<CashHandoverDto>>.Ok(list.Select(MapHandover).ToList());
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private async Task<bool> VerifyManagerPinAsync(string plainPin)
    {
        var managers    = await _userManager.GetUsersInRoleAsync("Manager");
        var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

        return managers.Concat(superAdmins)
            .Where(u => u.IsActive && !u.IsDeleted && u.ManagerPin is not null)
            .Any(u => BCrypt.Net.BCrypt.Verify(plainPin, u.ManagerPin!));
    }

    private static CashHandoverDto MapHandover(CashHandover h) => new()
    {
        Id                = h.Id,
        CashierId         = h.CashierId,
        CashierName       = h.Cashier is not null
            ? $"{h.Cashier.FullNameAr} / {h.Cashier.FullNameEn}"
            : string.Empty,
        ManagerId         = h.ManagerId,
        ManagerName       = h.Manager is not null
            ? $"{h.Manager.FullNameAr} / {h.Manager.FullNameEn}"
            : null,
        ShiftId           = h.ShiftId,
        OpenFloat         = h.OpenFloat,
        CashSalesTotal    = h.CashSalesTotal,
        CashRefundsTotal  = h.CashRefundsTotal,
        ExpectedCash      = h.ExpectedCash,
        ActualCash        = h.ActualCash,
        Difference        = h.Difference,
        Status            = h.Status,
        OpenedAt          = h.OpenedAt,
        ClosedAt          = h.ClosedAt,
        ManagerApprovedAt = h.ManagerApprovedAt,
        CashierNotes      = h.CashierNotes,
        ManagerNotes      = h.ManagerNotes,
    };
}
