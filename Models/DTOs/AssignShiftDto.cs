using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

public class AssignShiftDto
{
    [Required]
    public int      EmployeeId { get; set; }

    [Required]
    public int      TemplateId { get; set; }

    [Required]
    public DateTime ShiftDate  { get; set; }
}
