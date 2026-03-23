using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantMS.Controllers;

public class LanguageController : Controller
{
    private static readonly HashSet<string> _allowed =
        new(StringComparer.OrdinalIgnoreCase) { "ar", "ar-SA", "en", "en-US" };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        if (!_allowed.Contains(culture))
            culture = "ar";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
            new CookieOptions
            {
                Expires     = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly    = false,   // JS reads it for UI state
                SameSite    = SameSiteMode.Lax,
            });

        return LocalRedirect(returnUrl);
    }
}
