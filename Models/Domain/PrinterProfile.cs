namespace RestaurantMS.Models.Domain;

public class PrinterProfile : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public PrinterType PrinterType { get; set; }
    public PaperWidth PaperWidth { get; set; }
    public PrinterLocation Location { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool PrintLogo { get; set; } = true;
    public bool PrintArabic { get; set; } = true;
    public int Copies { get; set; } = 1;
}
