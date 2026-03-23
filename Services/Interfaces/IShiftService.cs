using RestaurantMS.Helpers;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Services.Interfaces;

public interface IShiftService
{
    Task<ApiResponse<EmployeeShiftDto>>       ClockInAsync(int employeeId, int templateId);
    Task<ApiResponse<EmployeeShiftDto>>       ClockOutAsync(int shiftId, int employeeId);
    Task<ApiResponse<List<EmployeeShiftDto>>> GetScheduleAsync(DateTime from, DateTime to);
    Task<ApiResponse<bool>>                   AssignShiftAsync(AssignShiftDto dto, int managerId);
    Task<ApiResponse<EmployeeShiftDto?>>      GetActiveShiftAsync(int employeeId);
    Task<ApiResponse<List<EmployeeShiftDto>>> GetMyShiftsAsync(int employeeId, DateTime from, DateTime to);
}
