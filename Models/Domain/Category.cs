namespace RestaurantMS.Models.Domain;

public class Category : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
