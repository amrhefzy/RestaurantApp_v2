using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    private readonly IDashboardService _dashboard = dashboardService;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"]      = "Dashboard";
        ViewData["ActiveMenu"] = "Dashboard";

        var result = await _dashboard.GetDashboardDataAsync();
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var result = await _dashboard.GetDashboardDataAsync();
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveOrders()
    {
        var result = await _dashboard.GetActiveOrdersAsync();
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetHourlySales()
    {
        var result = await _dashboard.GetTodayHourlySalesAsync();
        return Json(result);
    }
}
