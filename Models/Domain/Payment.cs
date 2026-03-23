using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? CashReceived { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? ChangeGiven { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? ReferenceNumber { get; set; }

    // Navigation
    public PosOrder? Order { get; set; }
}
