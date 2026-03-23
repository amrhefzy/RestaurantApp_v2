using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "Settings";
        return View();
    }
}
