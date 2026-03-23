using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager,Cashier")]
public class CashHandoverController(IHandoverService handoverService) : Controller
{
    private readonly IHandoverService _handoverService = handoverService;

    private int UserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // ── Views ──────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Open()
    {
        ViewData["Title"]      = "Open Handover";
        ViewData["ActiveMenu"] = "CashHandover";

        var current = await _handoverService.GetCurrentHandoverAsync(UserId);
        if (current.Success && current.Data is not null)
        {
            TempData["Warning"] = "You already have an open handover.";
            return RedirectToAction(nameof(Close));
        }

        var history = await _handoverService.GetHandoverHistoryAsync(
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        ViewBag.RecentHandovers = history.Data ?? new();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open([FromBody] OpenHandoverDto dto)
    {
        var result = await _handoverService.OpenHandoverAsync(dto, UserId);
        return Json(result);
    }

    public async Task<IActionResult> Close()
    {
        ViewData["Title"]      = "Close Handover";
        ViewData["ActiveMenu"] = "CashHandover";

        var current = await _handoverService.GetCurrentHandoverAsync(UserId);
        if (!current.Success || current.Data is null)
        {
            TempData["Warning"] = "No open handover found.";
            return RedirectToAction(nameof(Open));
        }

        ViewBag.Handover = current.Data;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close([FromBody] CloseHandoverDto dto)
    {
        var result = await _handoverService.CloseHandoverAsync(dto, UserId);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve([FromBody] ApproveHandoverDto req)
    {
        var result = await _handoverService.ApproveHandoverAsync(
            req.HandoverId, req.ManagerPin, UserId, req.Notes);
        return Json(result);
    }

    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<IActionResult> History(DateTime? from, DateTime? to)
    {
        ViewData["Title"]      = "Handover History";
        ViewData["ActiveMenu"] = "CashHandover";
        ViewBag.From = (from ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        ViewBag.To   = (to   ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        return View();
    }

    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<IActionResult> Detail(int id)
    {
        ViewData["Title"]      = "Handover Detail";
        ViewData["ActiveMenu"] = "CashHandover";

        var result = await _handoverService.GetHandoverSummaryAsync(id);
        if (!result.Success || result.Data is null)
            return NotFound();

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(int id)
    {
        var result = await _handoverService.GetHandoverSummaryAsync(id);
        return Json(result);
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<IActionResult> GetHistory(DateTime from, DateTime to)
    {
        var result = await _handoverService.GetHandoverHistoryAsync(from, to);
        return Json(result);
    }
}

// ── Inline DTO ─────────────────────────────────────────────────────────────────
public record ApproveHandoverDto(int HandoverId, string ManagerPin, string? Notes);
