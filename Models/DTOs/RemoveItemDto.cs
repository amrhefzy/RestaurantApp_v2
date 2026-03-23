using System.ComponentModel.DataAnnotations;

namespace RestaurantMS.Models.DTOs;

public class RemoveItemDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public int ItemId  { get; set; }
}
