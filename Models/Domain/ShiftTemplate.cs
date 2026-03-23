using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class ShiftTemplate : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string NameAr { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string NameEn { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int MaxHours { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<EmployeeShift> EmployeeShifts { get; set; } = new List<EmployeeShift>();
}
