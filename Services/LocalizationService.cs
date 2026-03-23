using Microsoft.AspNetCore.Http;
using RestaurantMS.Services.Interfaces;

namespace RestaurantMS.Services;

public class LocalizationService(IHttpContextAccessor httpContextAccessor) : ILocalizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string GetCurrentCulture()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return "ar-SA";

        // 1. Try ASP.NET Core culture cookie (.AspNetCore.Culture → c=ar-SA|uic=ar-SA)
        if (ctx.Request.Cookies.TryGetValue(".AspNetCore.Culture", out var cookie)
            && !string.IsNullOrEmpty(cookie))
        {
            var culture = cookie
                .Split('|')
                .FirstOrDefault(p => p.StartsWith("c=", StringComparison.Ordinal))
                ?[2..];  // strip "c="

            if (!string.IsNullOrEmpty(culture))
                return culture;
        }

        // 2. Fall back to Accept-Language header
        var acceptLang = ctx.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(acceptLang))
        {
            var first = acceptLang.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(first))
                return first.Split(';').First().Trim(); // strip q-factor
        }

        return "ar-SA";
    }

    public bool IsArabic()
        => GetCurrentCulture().StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    public string Localize(string key)
        => key; // Extend with IStringLocalizer / .resx in a later step

    public string GetLocalizedName(string nameAr, string nameEn)
        => IsArabic() ? nameAr : nameEn;
}
