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
public class PosService(
    IUnitOfWork uow,
    IConfiguration config,
    UserManager<ApplicationUser> userManager) : IPosService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // ── Create Order ──────────────────────────────────────────────────────────
    public async Task<ApiResponse<PosOrderDto>> CreateOrderAsync(CreateOrderDto dto, int cashierId)
    {
        var orderNumber = await _uow.Orders.GetNextOrderNumberAsync();

        var order = new PosOrder
        {
            OrderNumber    = orderNumber,
            TableNumber    = dto.TableNumber,
            OrderType      = dto.OrderType,
            Status         = OrderStatus.Open,
            CashierId      = cashierId,
            SubTotal       = 0,
            TaxAmount      = 0,
            DiscountAmount = 0,
            TotalAmount    = 0,
        };

        await _uow.Orders.AddAsync(order);
        await _uow.CompleteAsync();

        var created = await _uow.Orders.GetOrderWithItemsAsync(order.Id);
        return ApiResponse<PosOrderDto>.Ok(MapOrder(created!), "Order created.");
    }

    // ── Add Item ──────────────────────────────────────────────────────────────
    public async Task<ApiResponse<PosOrderDto>> AddItemToOrderAsync(int orderId, AddOrderItemDto dto)
    {
        var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
        if (order is null)
            return ApiResponse<PosOrderDto>.Fail("Order not found.", 404);
        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
            return ApiResponse<PosOrderDto>.Fail("Cannot modify a closed order.");

        var menuItem = await _uow.MenuItems.GetByIdAsync(dto.MenuItemId);
        if (menuItem is null || !menuItem.IsAvailable)
            return ApiResponse<PosOrderDto>.Fail("Menu item not found or unavailable.", 404);

        // Merge into existing line if same item already in order
        var existingLine = order.OrderItems.FirstOrDefault(i => i.MenuItemId == dto.MenuItemId);
        if (existingLine is not null)
        {
            var tracked = await _uow.OrderItems.GetByIdTrackedAsync(existingLine.Id);
            if (tracked is not null)
            {
                tracked.Quantity   += dto.Quantity;
                tracked.TotalPrice  = tracked.Quantity * tracked.UnitPrice;
            }
        }
        else
        {
            await _uow.OrderItems.AddAsync(new PosOrderItem
            {
                OrderId    = orderId,
                MenuItemId = dto.MenuItemId,
                Quantity   = dto.Quantity,
                UnitPrice  = menuItem.Price,
                TotalPrice = menuItem.Price * dto.Quantity,
                Notes      = dto.Notes,
            });
        }

        await RecalculateTotalsAsync(orderId);
        await _uow.CompleteAsync();

        var updated = await _uow.Orders.GetOrderWithItemsAsync(orderId);
        return ApiResponse<PosOrderDto>.Ok(MapOrder(updated!));
    }

    // ── Remove Item ───────────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> RemoveItemFromOrderAsync(int orderId, int itemId)
    {
        var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
        if (order is null)
            return ApiResponse<bool>.Fail("Order not found.", 404);
        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
            return ApiResponse<bool>.Fail("Cannot modify a closed order.");

        var item = order.OrderItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return ApiResponse<bool>.Fail("Item not found in order.", 404);

        var tracked = await _uow.OrderItems.GetByIdTrackedAsync(itemId);
        if (tracked is not null)
            tracked.IsDeleted = true;

        await RecalculateTotalsAsync(orderId, excludeItemId: itemId);
        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true);
    }

    // ── Update Quantity ───────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> UpdateItemQuantityAsync(int orderId, int itemId, decimal qty)
    {
        if (qty <= 0)
            return ApiResponse<bool>.Fail("Quantity must be greater than zero.");

        var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
        if (order is null)
            return ApiResponse<bool>.Fail("Order not found.", 404);
        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
            return ApiResponse<bool>.Fail("Cannot modify a closed order.");

        var item = order.OrderItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return ApiResponse<bool>.Fail("Item not found.", 404);

        var tracked = await _uow.OrderItems.GetByIdTrackedAsync(itemId);
        if (tracked is not null)
        {
            tracked.Quantity   = qty;
            tracked.TotalPrice = tracked.UnitPrice * qty;
        }

        await RecalculateTotalsAsync(orderId);
        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true);
    }

    // ── Process Payment ───────────────────────────────────────────────────────
    public async Task<ApiResponse<PaymentResultDto>> ProcessPaymentAsync(
        int orderId, ProcessPaymentDto dto, int cashierId)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
            if (order is null)
                return ApiResponse<PaymentResultDto>.Fail("Order not found.", 404);
            if (order.Status == OrderStatus.Paid)
                return ApiResponse<PaymentResultDto>.Fail("Order is already paid.");
            if (order.Status == OrderStatus.Voided)
                return ApiResponse<PaymentResultDto>.Fail("Cannot pay a voided order.");
            if (!order.OrderItems.Any())
                return ApiResponse<PaymentResultDto>.Fail("Order has no items.");

            var taxRate            = _config.GetValue<decimal>("PosSettings:TaxRate", 14m) / 100m;
            var discountThreshold  = _config.GetValue<decimal>("PosSettings:DiscountThresholdPercent", 10m);
            var discountAmount     = dto.DiscountAmount ?? 0m;

            // Manager PIN required for high discounts
            if (discountAmount > 0 && order.SubTotal > 0)
            {
                var discountPct = (discountAmount / order.SubTotal) * 100m;
                if (discountPct > discountThreshold)
                {
                    if (string.IsNullOrWhiteSpace(dto.ManagerPin))
                        return ApiResponse<PaymentResultDto>.Fail(
                            $"Discounts over {discountThreshold}% require a Manager PIN.");

                    if (!await VerifyManagerPinAsync(dto.ManagerPin))
                        return ApiResponse<PaymentResultDto>.Fail("Invalid Manager PIN.");
                }
            }

            // Recalculate with confirmed discount
            var subTotal   = order.OrderItems.Sum(i => i.TotalPrice);
            var taxAmount  = (subTotal - discountAmount) * taxRate;
            var total      = subTotal - discountAmount + taxAmount;

            if (dto.PaymentMethod == PaymentMethod.Cash)
            {
                var cashReceived = dto.CashReceived ?? 0m;
                if (cashReceived < total)
                    return ApiResponse<PaymentResultDto>.Fail(
                        $"Cash received ({cashReceived:F3}) is less than total ({total:F3}).");
            }

            var cashIn     = dto.CashReceived ?? 0m;
            var changeOut  = dto.PaymentMethod == PaymentMethod.Cash ? cashIn - total : 0m;

            // Update order (tracked)
            var trackedOrder = await _uow.Orders.GetByIdTrackedAsync(orderId);
            if (trackedOrder is not null)
            {
                trackedOrder.SubTotal       = subTotal;
                trackedOrder.DiscountAmount = discountAmount;
                trackedOrder.TaxAmount      = taxAmount;
                trackedOrder.TotalAmount    = total;
                trackedOrder.Status         = OrderStatus.Paid;
                trackedOrder.PaidAt         = DateTime.UtcNow;
            }

            // Create payment record
            await _uow.Payments.AddAsync(new Payment
            {
                OrderId       = orderId,
                PaymentMethod = dto.PaymentMethod,
                Amount        = total,
                CashReceived  = dto.PaymentMethod == PaymentMethod.Cash ? cashIn  : null,
                ChangeGiven   = dto.PaymentMethod == PaymentMethod.Cash ? changeOut : null,
            });

            // Update open handover cash totals
            if (dto.PaymentMethod is PaymentMethod.Cash or PaymentMethod.Split)
            {
                var handover = await _uow.CashHandovers.GetOpenHandoverByCashierAsync(cashierId);
                if (handover is not null)
                {
                    var trackedHandover = await _uow.CashHandovers.GetByIdTrackedAsync(handover.Id);
                    if (trackedHandover is not null)
                        trackedHandover.CashSalesTotal += total;
                }
            }

            await _uow.CompleteAsync();
            await _uow.CommitTransactionAsync();

            return ApiResponse<PaymentResultDto>.Ok(new PaymentResultDto
            {
                OrderId       = orderId,
                OrderNumber   = order.OrderNumber,
                TotalAmount   = total,
                CashReceived  = cashIn,
                ChangeGiven   = changeOut,
                PaymentMethod = dto.PaymentMethod,
            });
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync();
            return ApiResponse<PaymentResultDto>.Fail($"Payment processing failed: {ex.Message}", 500);
        }
    }

    // ── Void Order ────────────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> VoidOrderAsync(int orderId, string reason, string managerPin)
    {
        if (!await VerifyManagerPinAsync(managerPin))
            return ApiResponse<bool>.Fail("Invalid Manager PIN.");

        var order = await _uow.Orders.GetByIdTrackedAsync(orderId);
        if (order is null)
            return ApiResponse<bool>.Fail("Order not found.", 404);
        if (order.Status == OrderStatus.Paid)
            return ApiResponse<bool>.Fail("Cannot void a paid order. Use a refund instead.");

        order.Status = OrderStatus.Voided;
        order.Notes  = string.IsNullOrWhiteSpace(order.Notes)
            ? $"[VOID] {reason}"
            : $"{order.Notes} | [VOID] {reason}";

        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true, "Order voided.");
    }

    // ── Hold / Recall ─────────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> HoldOrderAsync(int orderId)
    {
        var order = await _uow.Orders.GetByIdTrackedAsync(orderId);
        if (order is null)
            return ApiResponse<bool>.Fail("Order not found.", 404);
        if (order.Status != OrderStatus.Open)
            return ApiResponse<bool>.Fail("Only open orders can be placed on hold.");

        order.Status = OrderStatus.Held;
        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<PosOrderDto>> RecallHeldOrderAsync(int orderId)
    {
        var order = await _uow.Orders.GetByIdTrackedAsync(orderId);
        if (order is null)
            return ApiResponse<PosOrderDto>.Fail("Order not found.", 404);
        if (order.Status != OrderStatus.Held)
            return ApiResponse<PosOrderDto>.Fail("Order is not on hold.");

        order.Status = OrderStatus.Open;
        await _uow.CompleteAsync();

        var full = await _uow.Orders.GetOrderWithItemsAsync(orderId);
        return ApiResponse<PosOrderDto>.Ok(MapOrder(full!));
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<ApiResponse<List<PosOrderDto>>> GetActiveOrdersAsync()
    {
        var orders = await _uow.Orders.GetActiveOrdersAsync();
        return ApiResponse<List<PosOrderDto>>.Ok(orders.Select(MapOrder).ToList());
    }

    public async Task<ApiResponse<PosOrderDto>> GetOrderDetailsAsync(int orderId)
    {
        var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
        if (order is null)
            return ApiResponse<PosOrderDto>.Fail("Order not found.", 404);
        return ApiResponse<PosOrderDto>.Ok(MapOrder(order));
    }

    public async Task<ApiResponse<List<MenuItemDto>>> GetMenuAsync(int? categoryId = null, string? search = null)
    {
        IEnumerable<MenuItem> items;

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = await _uow.MenuItems.SearchMenuAsync(search);
        }
        else if (categoryId.HasValue)
        {
            items = await _uow.MenuItems.FindAsync(
                m => m.CategoryId == categoryId.Value && m.IsAvailable);
        }
        else
        {
            items = await _uow.MenuItems.FindAsync(m => m.IsAvailable);
        }

        return ApiResponse<List<MenuItemDto>>.Ok(items.Select(MapMenuItem).ToList());
    }

    public async Task<ApiResponse<MenuItemDto?>> GetMenuItemByBarcodeAsync(string barcode)
    {
        var item = await _uow.MenuItems.GetByBarcodeAsync(barcode);
        if (item is null)
            return ApiResponse<MenuItemDto?>.Fail("Item not found.", 404);
        return ApiResponse<MenuItemDto?>.Ok(MapMenuItem(item));
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private async Task RecalculateTotalsAsync(int orderId, int? excludeItemId = null)
    {
        var allItems = await _uow.OrderItems.FindAsync(i => i.OrderId == orderId);
        var items    = excludeItemId.HasValue
            ? allItems.Where(i => i.Id != excludeItemId.Value)
            : allItems;

        var taxRate  = _config.GetValue<decimal>("PosSettings:TaxRate", 14m) / 100m;
        var subTotal = items.Sum(i => i.TotalPrice);
        var taxAmt   = subTotal * taxRate;

        var trackedOrder = await _uow.Orders.GetByIdTrackedAsync(orderId);
        if (trackedOrder is null) return;

        trackedOrder.SubTotal    = subTotal;
        trackedOrder.TaxAmount   = taxAmt;
        trackedOrder.TotalAmount = subTotal + taxAmt - trackedOrder.DiscountAmount;
    }

    private async Task<bool> VerifyManagerPinAsync(string plainPin)
    {
        var managers    = await _userManager.GetUsersInRoleAsync("Manager");
        var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

        return managers.Concat(superAdmins)
            .Where(u => u.IsActive && !u.IsDeleted && u.ManagerPin is not null)
            .Any(u => BCrypt.Net.BCrypt.Verify(plainPin, u.ManagerPin!));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────
    private static PosOrderDto MapOrder(PosOrder o) => new()
    {
        Id             = o.Id,
        OrderNumber    = o.OrderNumber,
        TableNumber    = o.TableNumber,
        OrderType      = o.OrderType,
        Status         = o.Status,
        SubTotal       = o.SubTotal,
        TaxAmount      = o.TaxAmount,
        DiscountAmount = o.DiscountAmount,
        TotalAmount    = o.TotalAmount,
        CreatedAt      = o.CreatedAt,
        CashierName    = o.Cashier is not null
            ? $"{o.Cashier.FullNameAr} / {o.Cashier.FullNameEn}"
            : string.Empty,
        Items = o.OrderItems.Select(i => new OrderItemDto
        {
            Id             = i.Id,
            MenuItemId     = i.MenuItemId,
            MenuItemNameAr = i.MenuItem?.NameAr ?? string.Empty,
            MenuItemNameEn = i.MenuItem?.NameEn ?? string.Empty,
            Quantity       = i.Quantity,
            UnitPrice      = i.UnitPrice,
            TotalPrice     = i.TotalPrice,
            Notes          = i.Notes,
        }).ToList(),
    };

    private static MenuItemDto MapMenuItem(MenuItem m) => new()
    {
        Id             = m.Id,
        NameAr         = m.NameAr,
        NameEn         = m.NameEn,
        Price          = m.Price,
        CategoryId     = m.CategoryId,
        CategoryNameAr = m.Category?.NameAr ?? string.Empty,
        CategoryNameEn = m.Category?.NameEn ?? string.Empty,
        ImageUrl       = m.ImageUrl,
        Barcode        = m.Barcode,
        IsAvailable    = m.IsAvailable,
        DisplayOrder   = m.DisplayOrder,
    };
}
