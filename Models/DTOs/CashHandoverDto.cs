using RestaurantMS.Models.Domain;

namespace RestaurantMS.Models.DTOs;

public class CashHandoverDto
{
    public int             Id                { get; set; }
    public int             CashierId         { get; set; }
    public string          CashierName       { get; set; } = string.Empty;
    public int?            ManagerId         { get; set; }
    public string?         ManagerName       { get; set; }
    public int?            ShiftId           { get; set; }
    public decimal         OpenFloat         { get; set; }
    public decimal         CashSalesTotal    { get; set; }
    public decimal         CashRefundsTotal  { get; set; }
    public decimal         ExpectedCash      { get; set; }
    public decimal?        ActualCash        { get; set; }
    public decimal?        Difference        { get; set; }
    public HandoverStatus  Status            { get; set; }
    public DateTime        OpenedAt          { get; set; }
    public DateTime?       ClosedAt          { get; set; }
    public DateTime?       ManagerApprovedAt { get; set; }
    public string?         CashierNotes      { get; set; }
    public string?         ManagerNotes      { get; set; }
}
