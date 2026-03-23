namespace RestaurantMS.Models.DTOs;

public class HourlySalesDto
{
    public int     Hour        { get; set; }
    public int     OrderCount  { get; set; }
    public decimal TotalAmount { get; set; }
}
