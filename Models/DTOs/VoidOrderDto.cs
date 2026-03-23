using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

public class VoidOrderDto
{
    [Required]
    public int     OrderId    { get; set; }

    [Required]
    [MaxLength(500)]
    public string  Reason     { get; set; } = string.Empty;

    [Required]
    public string  ManagerPin { get; set; } = string.Empty;
}
