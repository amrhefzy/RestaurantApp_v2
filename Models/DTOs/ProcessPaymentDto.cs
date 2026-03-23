using System.ComponentModel.DataAnnotations;
using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class ProcessPaymentDto
{
    [Required]
    public PaymentMethod PaymentMethod  { get; set; }

    /// <summary>Required when PaymentMethod == Cash.</summary>
    public decimal?      CashReceived   { get; set; }

    public decimal?      DiscountAmount { get; set; }

    /// <summary>Required when DiscountAmount exceeds the configured threshold percent.</summary>
    public string?       ManagerPin     { get; set; }
}
