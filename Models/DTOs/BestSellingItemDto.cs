namespace RestaurantMS.Models.DTOs;

public class BestSellingItemDto
{
    public int     MenuItemId    { get; set; }
    public string  NameAr        { get; set; } = string.Empty;
    public string  NameEn        { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalRevenue  { get; set; }
    public int     OrderCount    { get; set; }
}
