namespace RestaurantMS.Models.Domain;

public enum OrderType
{
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3
}

public enum OrderStatus
{
    Open = 1,
    Confirmed = 2,
    InProgress = 3,
    Ready = 4,
    Delivered = 5,
    Paid = 6,
    Cancelled = 7,
    Refunded = 8
}

public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    DigitalWallet = 5,
    Complimentary = 6
}

public enum HandoverStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ShiftStatus
{
    Scheduled = 1,
    Active = 2,
    Completed = 3,
    Absent = 4,
    Cancelled = 5
}

public enum PrinterType
{
    Thermal = 1,
    LaserJet = 2,
    InkJet = 3
}

public enum PaperWidth
{
    Width58mm = 58,
    Width80mm = 80,
    Width112mm = 112
}

public enum PrinterLocation
{
    Cashier = 1,
    Kitchen = 2,
    Bar = 3,
    Reception = 4,
    Manager = 5
}
