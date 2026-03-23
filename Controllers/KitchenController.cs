using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager,KitchenStaff")]
public class KitchenController(IPosService posService) : Controller
{
    private readonly IPosService _pos = posService;

    // ── Kitchen Display ───────────────────────────────────────────────────────
    public IActionResult Display()
    {
        ViewData["Title"] = "Kitchen Display";
        return View();
    }

    // ── JSON endpoints ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var result = await _pos.GetKitchenOrdersAsync();
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPreparing([FromBody] KitchenActionDto req)
    {
        var result = await _pos.MarkPreparingAsync(req.OrderId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReady([FromBody] KitchenActionDto req)
    {
        var result = await _pos.MarkReadyAsync(req.OrderId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkServed([FromBody] KitchenActionDto req)
    {
        var result = await _pos.MarkServedAsync(req.OrderId);
        return Json(result);
    }
}

public record KitchenActionDto(int OrderId);
