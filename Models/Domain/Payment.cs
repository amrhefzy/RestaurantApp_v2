namespace RestaurantMS.Models.Domain;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal CashReceived { get; set; }
    public decimal ChangeGiven { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public int CashierId { get; set; }

    // Navigation
    public PosOrder? Order { get; set; }
    public ApplicationUser? Cashier { get; set; }
}
