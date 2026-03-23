using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantMS.Models.Domain;

public class EmployeeShift : BaseEntity
{
    public int EmployeeId { get; set; }

    public int TemplateId { get; set; }

    public DateTime ShiftDate { get; set; }

    public DateTime? ClockIn { get; set; }

    public DateTime? ClockOut { get; set; }

    public DateTime PlannedStart { get; set; }

    public DateTime PlannedEnd { get; set; }

    public int OvertimeMinutes { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;

    public int? ManagerId { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? Notes { get; set; }

    // Navigation
    public ApplicationUser? Employee { get; set; }
    public ShiftTemplate? Template { get; set; }
    public ApplicationUser? Manager { get; set; }
}
