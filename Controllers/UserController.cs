using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMS.Models.Domain;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    public UserController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "Users";
        var users = await _userManager.Users.Where(u => !u.IsDeleted).ToListAsync();
        return View(users);
    }
}
