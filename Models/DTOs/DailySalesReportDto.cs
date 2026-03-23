namespace RestaurantMS.Models.DTOs;

public class DailySalesReportDto
{
    public DateTime             Date           { get; set; }
    public int                  TotalOrders    { get; set; }
    public decimal              SubTotal       { get; set; }
    public decimal              TaxAmount      { get; set; }
    public decimal              DiscountAmount { get; set; }
    public decimal              NetSales       { get; set; }
    public decimal              CashTotal      { get; set; }
    public decimal              CardTotal      { get; set; }
    public int                  VoidedOrders   { get; set; }
    public List<BestSellingItemDto> TopItems   { get; set; } = new();
    public List<HourlySalesDto>    HourlySales { get; set; } = new();
}
