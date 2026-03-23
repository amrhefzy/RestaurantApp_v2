namespace RestaurantMS.Models.Domain;

public enum OrderType
{
    DineIn   = 1,
    Takeaway = 2,
    Delivery = 3
}

public enum OrderStatus
{
    // Legacy values — kept for backward compatibility
    Open   = 1,
    Paid   = 2,
    Voided = 3,
    Held   = 4,

    // State-machine workflow states
    SentToKitchen = 5,
    Preparing     = 6,
    Ready         = 7,
    Served        = 8,
    Closed        = 9,    // terminal — successfully completed (alias for Paid)
    Cancelled     = 10    // terminal — cancelled (alias for Voided)
}

public enum PaymentMethod
{
    Cash  = 1,
    Card  = 2,
    Split = 3
}

public enum HandoverStatus
{
    Open              = 1,
    PendingApproval   = 2,
    Closed            = 3,
    Flagged           = 4,
    Rejected          = 5   // Manager rejected — needs re-submission
}

public enum ShiftStatus
{
    Scheduled      = 1,
    Active         = 2,
    Completed      = 3,
    Absent         = 4,
    ClosingPending = 5   // Cashier initiated close, awaiting final approval
}

public enum PrinterType
{
    ThermalReceipt = 1,
    A4Invoice      = 2,
    KitchenTicket  = 3
}

public enum PaperWidth
{
    W58mm = 1,
    W80mm = 2,
    A4    = 3
}

public enum PrinterLocation
{
    Cashier    = 1,
    Kitchen    = 2,
    Bar        = 3,
    Management = 4
}
