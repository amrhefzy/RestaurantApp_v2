using System.ComponentModel.DataAnnotations;
using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class CreateOrderDto
{
    public string?    TableNumber { get; set; }

    [Required]
    public OrderType  OrderType   { get; set; }
}
