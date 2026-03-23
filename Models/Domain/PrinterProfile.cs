using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class PrinterProfile : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string Name { get; set; } = string.Empty;

    public PrinterType PrinterType { get; set; }

    public PaperWidth PaperWidth { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? IpAddress { get; set; }

    public int Port { get; set; } = 9100;

    public bool IsDefault { get; set; } = false;

    public PrinterLocation Location { get; set; }

    public bool IsActive { get; set; } = true;
}
