using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager,Accountant")]
public class ReportController(IReportService reportService) : Controller
{
    private readonly IReportService _reportService = reportService;

    private int UserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // ── Views ──────────────────────────────────────────────────────────────────

    public IActionResult Index(DateTime? from, DateTime? to)
    {
        ViewData["Title"]      = "Reports";
        ViewData["ActiveMenu"] = "Reports";
        ViewBag.From = (from ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        ViewBag.To   = (to   ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        return View();
    }

    public async Task<IActionResult> Daily(DateTime? date)
    {
        ViewData["Title"]      = "Daily Sales Report";
        ViewData["ActiveMenu"] = "Reports";
        var d      = date ?? DateTime.UtcNow.Date;
        var result = await _reportService.GetDailySalesReportAsync(d);
        ViewBag.Date   = d.ToString("yyyy-MM-dd");
        ViewBag.Report = result.Data;
        return View();
    }

    public async Task<IActionResult> ZReport(DateTime? date)
    {
        ViewData["Title"]      = "Z-Report";
        ViewData["ActiveMenu"] = "Reports";
        var d      = date ?? DateTime.UtcNow.Date;
        var result = await _reportService.GetZReportAsync(d);
        if (!result.Success || result.Data is null)
            return NotFound("Z-Report not available for this date.");

        var model = result.Data;
        model.GeneratedBy = User.Identity?.Name ?? "—";
        model.GeneratedAt = DateTime.UtcNow;

        Layout             = "~/Views/Shared/_LayoutPrint.cshtml";
        ViewData["Layout"] = "~/Views/Shared/_LayoutPrint.cshtml";
        return View(model);
    }

    public async Task<IActionResult> XReport(int? shiftId)
    {
        ViewData["Title"]      = "X-Report";
        ViewData["ActiveMenu"] = "Reports";
        var result = await _reportService.GetXReportAsync(shiftId);
        ViewBag.Report = result.Data;
        return View();
    }

    // ── JSON endpoints ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetDailySalesData(DateTime? date)
    {
        var result = await _reportService.GetDailySalesReportAsync(date ?? DateTime.UtcNow.Date);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetBestSellers(DateTime? from, DateTime? to, int top = 10)
    {
        var f = from ?? DateTime.UtcNow.AddDays(-30);
        var t = to   ?? DateTime.UtcNow;
        var result = await _reportService.GetBestSellingItemsAsync(f, t, top);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetHourlySales(DateTime? date)
    {
        var result = await _reportService.GetHourlySalesAsync(date ?? DateTime.UtcNow.Date);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateTime? date)
    {
        var d      = date ?? DateTime.UtcNow.Date;
        var result = await _reportService.ExportDailyReportExcelAsync(d);

        if (!result.Success || result.Data is null)
            return NotFound(result.Message);

        return File(result.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DailyReport_{d:yyyyMMdd}.xlsx");
    }
}
