using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

/// <summary>Controller-level DTO: includes OrderId alongside AddOrderItemDto fields.</summary>
public class AddItemRequestDto
{
    [Required]
    public int     OrderId    { get; set; }

    [Required]
    public int     MenuItemId { get; set; }

    [Required]
    [Range(0.001, 999)]
    public decimal Quantity   { get; set; }

    [MaxLength(300)]
    public string? Notes      { get; set; }
}
