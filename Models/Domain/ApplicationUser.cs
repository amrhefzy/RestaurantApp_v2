using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RestaurantMS.Models.Domain;

public class ApplicationUser : IdentityUser<int>
{
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string FullNameAr { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string FullNameEn { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt-hashed Manager PIN for approval flows.
    /// </summary>
    [Column(TypeName = "nvarchar(200)")]
    public string? ManagerPin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
