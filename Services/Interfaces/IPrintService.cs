using RestaurantMS.Helpers;

namespace RestaurantMS.Services.Interfaces;

public interface IPrintService
{
    Task<ApiResponse<bool>> PrintReceiptAsync(int orderId, int? printerId = null);
    Task<ApiResponse<bool>> PrintKitchenTicketAsync(int orderId);
    Task<ApiResponse<bool>> PrintInvoiceA4Async(int orderId);
    Task<ApiResponse<bool>> PrintZReportAsync(DateTime date, int? printerId = null);
    Task<ApiResponse<bool>> PrintXReportAsync(int? shiftId = null);
    Task<ApiResponse<bool>> TestPrintAsync(int printerId);
}
