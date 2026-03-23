using RestaurantMS.Helpers;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Services.Interfaces;

public interface IReportService
{
    Task<ApiResponse<DailySalesReportDto>>    GetDailySalesReportAsync(DateTime date);
    Task<ApiResponse<List<BestSellingItemDto>>> GetBestSellingItemsAsync(DateTime from, DateTime to, int top = 10);
    Task<ApiResponse<ZReportDto>>             GetZReportAsync(DateTime date);
    Task<ApiResponse<XReportDto>>             GetXReportAsync(int? shiftId = null);
    Task<ApiResponse<List<HourlySalesDto>>>   GetHourlySalesAsync(DateTime date);
    Task<ApiResponse<byte[]>>                 ExportDailyReportExcelAsync(DateTime date);
}
