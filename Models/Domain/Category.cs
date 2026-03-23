using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class Category : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string NameAr { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string NameEn { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(100)")]
    public string? IconClass { get; set; }

    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
