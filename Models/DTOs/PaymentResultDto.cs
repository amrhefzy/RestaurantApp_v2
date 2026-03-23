using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class PaymentResultDto
{
    public int           OrderId       { get; set; }
    public string        OrderNumber   { get; set; } = string.Empty;
    public decimal       TotalAmount   { get; set; }
    public decimal       CashReceived  { get; set; }
    public decimal       ChangeGiven   { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string?       ReceiptUrl    { get; set; }
}
