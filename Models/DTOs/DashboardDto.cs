namespace RestaurantMS.Models.DTOs;

public class DashboardDto
{
    public decimal TodaySales        { get; set; }
    public int     TodayOrders       { get; set; }
    public int     ActiveOrders      { get; set; }
    public int     ActiveShifts      { get; set; }
    public decimal AvgOrderValue     { get; set; }
    public decimal PendingDifference { get; set; }   // flagged handover differences
    public int     VoidedToday       { get; set; }
    public int     PendingHandovers  { get; set; }

    public List<HourlySalesDto>    HourlySales { get; set; } = new();
    public List<BestSellingItemDto> BestSellers { get; set; } = new();
    public List<ActiveOrderSummaryDto> ActiveOrdersList { get; set; } = new();
}

public class ActiveOrderSummaryDto
{
    public int         Id          { get; set; }
    public string      OrderNumber { get; set; } = string.Empty;
    public string      OrderType   { get; set; } = string.Empty;
    public string      Status      { get; set; } = string.Empty;
    public string      StatusClass { get; set; } = string.Empty;
    public decimal     Total       { get; set; }
    public int         ItemCount   { get; set; }
    public string      TableNumber { get; set; } = string.Empty;
    public DateTime    CreatedAt   { get; set; }
    public int         ElapsedMin  { get; set; }
}
