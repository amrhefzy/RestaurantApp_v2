namespace RestaurantMS.Models.DTOs;

public class ZReportDto : DailySalesReportDto
{
    public string   GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool     IsClosed    { get; set; }
}
