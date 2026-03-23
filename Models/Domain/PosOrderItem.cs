using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class PosOrderItem : BaseEntity
{
    public int OrderId { get; set; }

    public int MenuItemId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "nvarchar(300)")]
    public string? Notes { get; set; }

    // Navigation
    public PosOrder? Order { get; set; }
    public MenuItem? MenuItem { get; set; }
}
