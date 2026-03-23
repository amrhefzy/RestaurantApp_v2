namespace RestaurantMS.Services.Interfaces;

public interface ILocalizationService
{
    string GetCurrentCulture();
    bool   IsArabic();
    string Localize(string key);
    string GetLocalizedName(string nameAr, string nameEn);
}
