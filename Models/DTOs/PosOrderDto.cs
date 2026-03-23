using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class PosOrderDto
{
    public int             Id             { get; set; }
    public string          OrderNumber    { get; set; } = string.Empty;
    public string?         TableNumber    { get; set; }
    public OrderType       OrderType      { get; set; }
    public OrderStatus     Status         { get; set; }
    public List<OrderItemDto> Items       { get; set; } = new();
    public decimal         SubTotal       { get; set; }
    public decimal         TaxAmount      { get; set; }
    public decimal         DiscountAmount { get; set; }
    public decimal         TotalAmount    { get; set; }
    public string          CashierName    { get; set; } = string.Empty;
    public DateTime        CreatedAt      { get; set; }
}
