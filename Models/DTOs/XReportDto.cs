namespace RestaurantMS.Models.DTOs;

public class XReportDto
{
    public int?    ShiftId     { get; set; }
    public DateTime From       { get; set; }
    public DateTime To         { get; set; }
    public int     TotalOrders { get; set; }
    public decimal SubTotal    { get; set; }
    public decimal TaxAmount   { get; set; }
    public decimal NetSales    { get; set; }
    public decimal CashTotal   { get; set; }
    public decimal CardTotal   { get; set; }
    public List<HourlySalesDto> HourlySales { get; set; } = new();
}
