using RestaurantMS.Helpers;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Services.Interfaces;

public interface IHandoverService
{
    Task<ApiResponse<CashHandoverDto>>        OpenHandoverAsync(OpenHandoverDto dto, int cashierId);
    Task<ApiResponse<CashHandoverDto>>        GetCurrentHandoverAsync(int cashierId);
    Task<ApiResponse<CashHandoverSummaryDto>> GetHandoverSummaryAsync(int handoverId);
    Task<ApiResponse<bool>>                   CloseHandoverAsync(CloseHandoverDto dto, int cashierId);
    Task<ApiResponse<bool>>                   ApproveHandoverAsync(int handoverId, string managerPin, int managerId, string? notes);
    Task<ApiResponse<List<CashHandoverDto>>>  GetHandoverHistoryAsync(DateTime from, DateTime to);
}
