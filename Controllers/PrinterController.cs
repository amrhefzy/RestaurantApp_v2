using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager")]
public class PrinterController : Controller
{
    public IActionResult Index()
    {
        ViewData["ActiveMenu"] = "Printer";
        return View();
    }
}
