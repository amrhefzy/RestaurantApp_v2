using RestaurantMS.Helpers;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardDto>> GetDashboardDataAsync();
    Task<ApiResponse<List<ActiveOrderSummaryDto>>> GetActiveOrdersAsync();
    Task<ApiResponse<List<HourlySalesDto>>> GetTodayHourlySalesAsync();
}
