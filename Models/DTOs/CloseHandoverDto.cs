using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

public class CloseHandoverDto
{
    [Required]
    public int     HandoverId    { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Actual cash cannot be negative.")]
    public decimal ActualCash    { get; set; }

    public string? CashierNotes  { get; set; }
}
