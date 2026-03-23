using RestaurantMS.Helpers;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Services;

/// <summary>
/// Stub print service. Replace with a real thermal-printer integration
/// (e.g. ESC/POS over TCP or a Windows printer driver) as needed.
/// </summary>
public class PrintService : IPrintService
{
    public Task<ApiResponse<bool>> PrintReceiptAsync(int orderId, int? printerId = null)
        => Task.FromResult(ApiResponse<bool>.Ok(true, "Print receipt queued."));

    public Task<ApiResponse<bool>> PrintKitchenTicketAsync(int orderId)
        => Task.FromResult(ApiResponse<bool>.Ok(true, "Kitchen ticket queued."));

    public Task<ApiResponse<bool>> PrintInvoiceA4Async(int orderId)
        => Task.FromResult(ApiResponse<bool>.Ok(true, "A4 invoice queued."));

    public Task<ApiResponse<bool>> PrintZReportAsync(DateTime date, int? printerId = null)
        => Task.FromResult(ApiResponse<bool>.Ok(true, "Z-Report print queued."));

    public Task<ApiResponse<bool>> PrintXReportAsync(int? shiftId = null)
        => Task.FromResult(ApiResponse<bool>.Ok(true, "X-Report print queued."));

    public Task<ApiResponse<bool>> TestPrintAsync(int printerId)
        => Task.FromResult(ApiResponse<bool>.Ok(true, "Test print queued."));
}
