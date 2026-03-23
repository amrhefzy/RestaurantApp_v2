namespace RestaurantMS.Models.Domain;

public class PosOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int? TableNumber { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public int CashierId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation
    public ApplicationUser? Cashier { get; set; }
    public ICollection<PosOrderItem> OrderItems { get; set; } = new List<PosOrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
