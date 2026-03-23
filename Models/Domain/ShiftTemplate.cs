namespace RestaurantMS.Models.Domain;

public class ShiftTemplate : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxHours { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<EmployeeShift> EmployeeShifts { get; set; } = new List<EmployeeShift>();
}
