using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class PosOrder : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(20)")]
    public string? TableNumber { get; set; }

    public OrderType OrderType { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public int CashierId { get; set; }

    public int? ShiftId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? Notes { get; set; }

    public DateTime? PaidAt { get; set; }

    // Navigation
    public ApplicationUser? Cashier { get; set; }
    public ICollection<PosOrderItem> OrderItems { get; set; } = new List<PosOrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
