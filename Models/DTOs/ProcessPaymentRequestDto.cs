using System.ComponentModel.DataAnnotations;
using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

/// <summary>Controller-level DTO: includes OrderId alongside ProcessPaymentDto fields.</summary>
public class ProcessPaymentRequestDto
{
    [Required]
    public int           OrderId        { get; set; }

    [Required]
    public PaymentMethod PaymentMethod  { get; set; }

    public decimal?      CashReceived   { get; set; }
    public decimal?      DiscountAmount { get; set; }
    public string?       ManagerPin     { get; set; }
}
