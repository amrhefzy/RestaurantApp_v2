using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager,Cashier,Waiter,KitchenStaff")]
public class ShiftController(IShiftService shiftService) : Controller
{
    private readonly IShiftService _shiftService = shiftService;

    private int UserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // ── Views ──────────────────────────────────────────────────────────────────

    [Authorize(Roles = "SuperAdmin,Manager")]
    public IActionResult Schedule()
    {
        ViewData["Title"]      = "Shift Schedule";
        ViewData["ActiveMenu"] = "Shifts";
        return View();
    }

    public IActionResult ClockIn()
    {
        ViewData["Title"]      = "Clock In / Out";
        ViewData["ActiveMenu"] = "Shifts";
        return View();
    }

    public async Task<IActionResult> MyShifts(DateTime? from, DateTime? to)
    {
        ViewData["Title"]      = "My Shifts";
        ViewData["ActiveMenu"] = "Shifts";
        ViewBag.From = (from ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        ViewBag.To   = (to   ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        return View();
    }

    [Authorize(Roles = "SuperAdmin,Manager")]
    public IActionResult Report()
    {
        ViewData["Title"]      = "Shift Report";
        ViewData["ActiveMenu"] = "Shifts";
        return View();
    }

    // ── AJAX endpoints ─────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end)
    {
        var result = await _shiftService.GetScheduleAsync(start, end);
        if (!result.Success)
            return Json(new List<object>());

        // Transform to FullCalendar event format
        var events = (result.Data ?? new()).Select(s => new
        {
            id    = s.Id,
            title = $"{s.EmployeeNameEn} — {s.TemplateNameEn}",
            start = s.PlannedStart.ToString("o"),
            end   = s.PlannedEnd.ToString("o"),
            extendedProps = new
            {
                employeeId  = s.EmployeeId,
                templateId  = s.TemplateId,
                status      = (int)s.Status,
                clockIn     = s.ClockIn?.ToString("o"),
                clockOut    = s.ClockOut?.ToString("o"),
                overtime    = s.OvertimeMinutes,
                notes       = s.Notes,
            },
            backgroundColor = s.Status switch
            {
                RestaurantMS.Models.Domain.ShiftStatus.Active    => "#27ae60",
                RestaurantMS.Models.Domain.ShiftStatus.Completed => "#2980b9",
                RestaurantMS.Models.Domain.ShiftStatus.Absent    => "#e74c3c",
                _                                                => "#95a5a6",
            },
        });

        return Json(events);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign([FromBody] AssignShiftDto dto)
    {
        var result = await _shiftService.AssignShiftAsync(dto, UserId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockIn([FromBody] ClockInDto req)
    {
        var result = await _shiftService.ClockInAsync(req.EmployeeId, req.TemplateId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutDto req)
    {
        var result = await _shiftService.ClockOutAsync(req.ShiftId, UserId);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyShifts(DateTime from, DateTime to)
    {
        var result = await _shiftService.GetMyShiftsAsync(UserId, from, to);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveShift()
    {
        var result = await _shiftService.GetActiveShiftAsync(UserId);
        return Json(result);
    }
}

// ── Inline micro-DTOs ─────────────────────────────────────────────────────────
public record ClockInDto(int EmployeeId, int TemplateId);
public record ClockOutDto(int ShiftId);
