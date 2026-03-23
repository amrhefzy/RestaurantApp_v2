namespace RestaurantMS.Models.DTOs;

public class MenuItemDto
{
    public int     Id              { get; set; }
    public string  NameAr          { get; set; } = string.Empty;
    public string  NameEn          { get; set; } = string.Empty;
    public decimal Price           { get; set; }
    public int     CategoryId      { get; set; }
    public string  CategoryNameAr  { get; set; } = string.Empty;
    public string  CategoryNameEn  { get; set; } = string.Empty;
    public string? ImageUrl        { get; set; }
    public string? Barcode         { get; set; }
    public bool    IsAvailable     { get; set; }
    public int     DisplayOrder    { get; set; }
}
