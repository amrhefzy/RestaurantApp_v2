namespace RestaurantMS.Models.Domain;

public class PosOrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public PosOrder? Order { get; set; }
    public MenuItem? MenuItem { get; set; }
}
