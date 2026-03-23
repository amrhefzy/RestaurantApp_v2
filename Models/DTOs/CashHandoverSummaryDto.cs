using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class CashHandoverSummaryDto
{
    public int            Id               { get; set; }
    public string         CashierName      { get; set; } = string.Empty;
    public decimal        OpenFloat        { get; set; }
    public decimal        CashSalesTotal   { get; set; }
    public decimal        CashRefundsTotal { get; set; }
    public decimal        ExpectedCash     { get; set; }
    public decimal?       ActualCash       { get; set; }
    public decimal?       Difference       { get; set; }
    public HandoverStatus Status           { get; set; }
    public DateTime       OpenedAt         { get; set; }
    public DateTime?      ClosedAt         { get; set; }
    public int            TotalOrders      { get; set; }
    public decimal        TotalCardSales   { get; set; }
}
