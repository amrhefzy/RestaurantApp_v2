namespace RestaurantMS.Models.Domain;

public class MenuItem : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? Barcode { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? ImagePath { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    // Navigation
    public Category? Category { get; set; }
    public ICollection<PosOrderItem> OrderItems { get; set; } = new List<PosOrderItem>();
}
