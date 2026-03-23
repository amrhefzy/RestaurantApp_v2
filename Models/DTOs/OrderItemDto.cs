namespace RestaurantMS.Models.DTOs;

public class OrderItemDto
{
    public int     Id               { get; set; }
    public int     MenuItemId       { get; set; }
    public string  MenuItemNameAr   { get; set; } = string.Empty;
    public string  MenuItemNameEn   { get; set; } = string.Empty;
    public decimal Quantity         { get; set; }
    public decimal UnitPrice        { get; set; }
    public decimal TotalPrice       { get; set; }
    public string? Notes            { get; set; }
}
