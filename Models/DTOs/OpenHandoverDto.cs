using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

public class OpenHandoverDto
{
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Opening float cannot be negative.")]
    public decimal OpenFloat { get; set; }
}
