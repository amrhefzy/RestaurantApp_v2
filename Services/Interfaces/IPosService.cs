using RestaurantMS.Helpers;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Services.Interfaces;

public interface IPosService
{
    Task<ApiResponse<PosOrderDto>>      CreateOrderAsync(CreateOrderDto dto, int cashierId);
    Task<ApiResponse<PosOrderDto>>      AddItemToOrderAsync(int orderId, AddOrderItemDto dto);
    Task<ApiResponse<bool>>             RemoveItemFromOrderAsync(int orderId, int itemId);
    Task<ApiResponse<bool>>             UpdateItemQuantityAsync(int orderId, int itemId, decimal qty);
    Task<ApiResponse<PaymentResultDto>> ProcessPaymentAsync(int orderId, ProcessPaymentDto dto, int cashierId);
    Task<ApiResponse<bool>>             VoidOrderAsync(int orderId, string reason, string managerPin);
    Task<ApiResponse<bool>>             HoldOrderAsync(int orderId);
    Task<ApiResponse<PosOrderDto>>      RecallHeldOrderAsync(int orderId);
    Task<ApiResponse<List<PosOrderDto>>> GetActiveOrdersAsync();
    Task<ApiResponse<PosOrderDto>>      GetOrderDetailsAsync(int orderId);
    Task<ApiResponse<List<MenuItemDto>>> GetMenuAsync(int? categoryId = null, string? search = null);
    Task<ApiResponse<MenuItemDto?>>     GetMenuItemByBarcodeAsync(string barcode);

    // ── State-machine transitions ──────────────────────────────────────────
    Task<ApiResponse<bool>> SendToKitchenAsync(int orderId);
    Task<ApiResponse<bool>> MarkPreparingAsync(int orderId);
    Task<ApiResponse<bool>> MarkReadyAsync(int orderId);
    Task<ApiResponse<bool>> MarkServedAsync(int orderId);
    Task<ApiResponse<List<PosOrderDto>>> GetKitchenOrdersAsync();
}
