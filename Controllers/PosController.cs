using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Helpers;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager,Cashier")]
public class PosController(IPosService posService) : Controller
{
    private readonly IPosService _posService = posService;

    private int CashierId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // ── Views ─────────────────────────────────────────────────────────────────

    public IActionResult Index()
    {
        ViewData["Title"]      = "POS";
        ViewData["ActiveMenu"] = "Pos";
        return View();
    }

    public IActionResult Touch()
    {
        ViewData["Title"] = "POS Touch";
        return View();
    }

    // ── Menu data ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetMenu(int? categoryId, string? search)
    {
        var result = await _posService.GetMenuAsync(categoryId, search);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        // Returns all active categories with their items for sidebar tabs
        var result = await _posService.GetMenuAsync();
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetItemByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Json(ApiResponse<object>.Fail("Barcode is required.", 400));

        var result = await _posService.GetMenuItemByBarcodeAsync(barcode);
        return Json(result);
    }

    // ── Order lifecycle ───────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var result = await _posService.CreateOrderAsync(dto, CashierId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequestDto req)
    {
        if (req.OrderId <= 0)
            return Json(ApiResponse<object>.Fail("OrderId is required."));

        var dto = new AddOrderItemDto
        {
            MenuItemId = req.MenuItemId,
            Quantity   = req.Quantity,
            Notes      = req.Notes,
        };

        var result = await _posService.AddItemToOrderAsync(req.OrderId, dto);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveItemDto req)
    {
        var result = await _posService.RemoveItemFromOrderAsync(req.OrderId, req.ItemId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQty([FromBody] UpdateQtyDto req)
    {
        var result = await _posService.UpdateItemQuantityAsync(req.OrderId, req.ItemId, req.Quantity);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequestDto req)
    {
        var dto = new ProcessPaymentDto
        {
            PaymentMethod  = req.PaymentMethod,
            CashReceived   = req.CashReceived,
            DiscountAmount = req.DiscountAmount,
            ManagerPin     = req.ManagerPin,
        };

        var result = await _posService.ProcessPaymentAsync(req.OrderId, dto, CashierId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidOrder([FromBody] VoidOrderDto req)
    {
        var result = await _posService.VoidOrderAsync(req.OrderId, req.Reason, req.ManagerPin);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoldOrder([FromBody] HoldOrderDto req)
    {
        var result = await _posService.HoldOrderAsync(req.OrderId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecallOrder([FromBody] HoldOrderDto req)
    {
        var result = await _posService.RecallHeldOrderAsync(req.OrderId);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveOrders()
    {
        var result = await _posService.GetActiveOrdersAsync();
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        var result = await _posService.GetOrderDetailsAsync(orderId);
        return Json(result);
    }
}

// ── Inline micro-DTOs (controller-only, no service dependency) ────────────────
public record UpdateQtyDto(int OrderId, int ItemId, decimal Quantity);
public record HoldOrderDto(int OrderId);
