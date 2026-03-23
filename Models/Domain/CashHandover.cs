namespace RestaurantMS.Models.Domain;

public class CashHandover : BaseEntity
{
    public int CashierId { get; set; }
    public int? ApproverId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCardSales { get; set; }
    public decimal TotalSales { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Difference { get; set; }
    public HandoverStatus Status { get; set; } = HandoverStatus.Pending;
    public DateTime HandoverDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? CashierNotes { get; set; }
    public string? ApproverNotes { get; set; }

    // Navigation
    public ApplicationUser? Cashier { get; set; }
    public ApplicationUser? Approver { get; set; }
}
