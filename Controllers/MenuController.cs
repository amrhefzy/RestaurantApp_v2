using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Repositories.Interfaces;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin,Manager")]
public class MenuController : Controller
{
    private readonly IUnitOfWork _uow;
    public MenuController(IUnitOfWork uow) => _uow = uow;

    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "Menu";
        var items = await _uow.MenuItems.GetAllAsync();
        return View(items);
    }
}
