using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class MenuItem : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string NameAr { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string NameEn { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(500)")]
    public string? DescriptionAr { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? DescriptionEn { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal Price { get; set; }

    public int CategoryId { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? ImageUrl { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? Barcode { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public Category? Category { get; set; }
    public ICollection<PosOrderItem> OrderItems { get; set; } = new List<PosOrderItem>();
}
