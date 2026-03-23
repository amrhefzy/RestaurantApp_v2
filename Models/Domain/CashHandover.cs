using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class CashHandover : BaseEntity
{
    public int CashierId { get; set; }

    public int? ManagerId { get; set; }

    public int? ShiftId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal OpenFloat { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal CashSalesTotal { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal CashRefundsTotal { get; set; }

    /// <summary>
    /// Computed: OpenFloat + CashSalesTotal - CashRefundsTotal
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal ExpectedCash => OpenFloat + CashSalesTotal - CashRefundsTotal;

    [Column(TypeName = "decimal(18,3)")]
    public decimal? ActualCash { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? Difference { get; set; }

    public HandoverStatus Status { get; set; } = HandoverStatus.Open;

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime? ManagerApprovedAt { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? CashierNotes { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? ManagerNotes { get; set; }

    // Navigation
    public ApplicationUser? Cashier { get; set; }
    public ApplicationUser? Manager { get; set; }
}
