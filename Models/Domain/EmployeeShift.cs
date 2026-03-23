namespace RestaurantMS.Models.Domain;

public class EmployeeShift : BaseEntity
{
    public int EmployeeId { get; set; }
    public int ShiftTemplateId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;
    public decimal? OvertimeHours { get; set; }
    public string? Notes { get; set; }
    public int? ApprovedById { get; set; }

    // Navigation
    public ApplicationUser? Employee { get; set; }
    public ShiftTemplate? ShiftTemplate { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
}
