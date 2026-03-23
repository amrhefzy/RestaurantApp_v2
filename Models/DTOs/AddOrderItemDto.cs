using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

public class AddOrderItemDto
{
    [Required]
    public int     MenuItemId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity   { get; set; }

    public string? Notes      { get; set; }
}
