using RestaurantMS.Helpers;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;
using RestaurantMS.Repositories.Interfaces;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Services;

public class ShiftService(IUnitOfWork uow) : IShiftService
{
    private readonly IUnitOfWork _uow = uow;

    // ── Clock In ──────────────────────────────────────────────────────────────
    public async Task<ApiResponse<EmployeeShiftDto>> ClockInAsync(int employeeId, int templateId)
    {
        var activeShift = await _uow.Shifts.GetActiveShiftByEmployeeAsync(employeeId);
        if (activeShift is not null)
            return ApiResponse<EmployeeShiftDto>.Fail("Employee already has an active shift.");

        var template = await _uow.ShiftTemplates.GetByIdAsync(templateId);
        if (template is null || !template.IsActive)
            return ApiResponse<EmployeeShiftDto>.Fail("Shift template not found or inactive.", 404);

        var now          = DateTime.UtcNow;
        var plannedStart = now.Date + template.StartTime;
        var plannedEnd   = plannedStart + (template.EndTime > template.StartTime
            ? template.EndTime - template.StartTime
            : TimeSpan.FromHours(24) - template.StartTime + template.EndTime);

        // Try to update an existing Scheduled shift for today, or create a new one
        var todayShifts = await _uow.Shifts.GetShiftsByDateRangeAsync(now.Date, now.Date.AddDays(1));
        var scheduled   = todayShifts.FirstOrDefault(s =>
            s.EmployeeId == employeeId && s.TemplateId == templateId &&
            s.Status == ShiftStatus.Scheduled);

        if (scheduled is not null)
        {
            var tracked = await _uow.Shifts.GetByIdTrackedAsync(scheduled.Id);
            if (tracked is not null)
            {
                tracked.ClockIn = now;
                tracked.Status  = ShiftStatus.Active;
                await _uow.CompleteAsync();

                var full = await _uow.Shifts.GetShiftWithTemplateAsync(tracked.Id);
                return ApiResponse<EmployeeShiftDto>.Ok(MapShift(full!), "Clocked in.");
            }
        }

        // No scheduled shift — create on the fly
        var shift = new EmployeeShift
        {
            EmployeeId      = employeeId,
            TemplateId      = templateId,
            ShiftDate       = now,
            ClockIn         = now,
            PlannedStart    = plannedStart,
            PlannedEnd      = plannedEnd,
            Status          = ShiftStatus.Active,
            OvertimeMinutes = 0,
        };

        await _uow.Shifts.AddAsync(shift);
        await _uow.CompleteAsync();

        var saved = await _uow.Shifts.GetShiftWithTemplateAsync(shift.Id);
        return ApiResponse<EmployeeShiftDto>.Ok(MapShift(saved!), "Clocked in.");
    }

    // ── Clock Out ─────────────────────────────────────────────────────────────
    public async Task<ApiResponse<EmployeeShiftDto>> ClockOutAsync(int shiftId, int employeeId)
    {
        var shift = await _uow.Shifts.GetByIdTrackedAsync(shiftId);
        if (shift is null)
            return ApiResponse<EmployeeShiftDto>.Fail("Shift not found.", 404);
        if (shift.EmployeeId != employeeId)
            return ApiResponse<EmployeeShiftDto>.Fail("This shift does not belong to the employee.", 403);
        if (shift.Status != ShiftStatus.Active)
            return ApiResponse<EmployeeShiftDto>.Fail("Shift is not active.");

        var now             = DateTime.UtcNow;
        var overtime        = now > shift.PlannedEnd
            ? (int)(now - shift.PlannedEnd).TotalMinutes
            : 0;

        shift.ClockOut        = now;
        shift.OvertimeMinutes = overtime;
        shift.Status          = ShiftStatus.Completed;

        await _uow.CompleteAsync();

        var full = await _uow.Shifts.GetShiftWithTemplateAsync(shiftId);
        return ApiResponse<EmployeeShiftDto>.Ok(MapShift(full!),
            overtime > 0 ? $"Clocked out. Overtime: {overtime} min." : "Clocked out.");
    }

    // ── Schedule ──────────────────────────────────────────────────────────────
    public async Task<ApiResponse<List<EmployeeShiftDto>>> GetScheduleAsync(DateTime from, DateTime to)
    {
        var shifts = await _uow.Shifts.GetShiftsByDateRangeAsync(from, to);
        return ApiResponse<List<EmployeeShiftDto>>.Ok(shifts.Select(MapShift).ToList());
    }

    // ── Assign Shift ──────────────────────────────────────────────────────────
    public async Task<ApiResponse<bool>> AssignShiftAsync(AssignShiftDto dto, int managerId)
    {
        var template = await _uow.ShiftTemplates.GetByIdAsync(dto.TemplateId);
        if (template is null || !template.IsActive)
            return ApiResponse<bool>.Fail("Shift template not found or inactive.", 404);

        // Prevent duplicate assignment for same employee + template + date
        var existing = await _uow.Shifts.FirstOrDefaultAsync(s =>
            s.EmployeeId == dto.EmployeeId &&
            s.TemplateId == dto.TemplateId &&
            s.ShiftDate.Date == dto.ShiftDate.Date);

        if (existing is not null)
            return ApiResponse<bool>.Fail("Employee already has this shift assigned on the specified date.");

        var plannedStart = dto.ShiftDate.Date + template.StartTime;
        var plannedEnd   = plannedStart + (template.EndTime > template.StartTime
            ? template.EndTime - template.StartTime
            : TimeSpan.FromHours(24) - template.StartTime + template.EndTime);

        await _uow.Shifts.AddAsync(new EmployeeShift
        {
            EmployeeId      = dto.EmployeeId,
            TemplateId      = dto.TemplateId,
            ShiftDate       = dto.ShiftDate.Date,
            PlannedStart    = plannedStart,
            PlannedEnd      = plannedEnd,
            Status          = ShiftStatus.Scheduled,
            ManagerId       = managerId,
            OvertimeMinutes = 0,
        });

        await _uow.CompleteAsync();
        return ApiResponse<bool>.Ok(true, "Shift assigned.");
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<ApiResponse<EmployeeShiftDto?>> GetActiveShiftAsync(int employeeId)
    {
        var shift = await _uow.Shifts.GetActiveShiftByEmployeeAsync(employeeId);
        if (shift is null)
            return ApiResponse<EmployeeShiftDto?>.Ok(null, "No active shift.");

        var full = await _uow.Shifts.GetShiftWithTemplateAsync(shift.Id);
        return ApiResponse<EmployeeShiftDto?>.Ok(MapShift(full!));
    }

    public async Task<ApiResponse<List<EmployeeShiftDto>>> GetMyShiftsAsync(
        int employeeId, DateTime from, DateTime to)
    {
        var all     = await _uow.Shifts.GetShiftsByDateRangeAsync(from, to);
        var mine    = all.Where(s => s.EmployeeId == employeeId).ToList();
        return ApiResponse<List<EmployeeShiftDto>>.Ok(mine.Select(MapShift).ToList());
    }

    // ── Mapping ───────────────────────────────────────────────────────────────
    private static EmployeeShiftDto MapShift(EmployeeShift s) => new()
    {
        Id              = s.Id,
        EmployeeId      = s.EmployeeId,
        EmployeeNameAr  = s.Employee?.FullNameAr ?? string.Empty,
        EmployeeNameEn  = s.Employee?.FullNameEn ?? string.Empty,
        TemplateId      = s.TemplateId,
        TemplateNameAr  = s.Template?.NameAr ?? string.Empty,
        TemplateNameEn  = s.Template?.NameEn ?? string.Empty,
        ShiftDate       = s.ShiftDate,
        ClockIn         = s.ClockIn,
        ClockOut        = s.ClockOut,
        PlannedStart    = s.PlannedStart,
        PlannedEnd      = s.PlannedEnd,
        OvertimeMinutes = s.OvertimeMinutes,
        Status          = s.Status,
        Notes           = s.Notes,
    };
}
