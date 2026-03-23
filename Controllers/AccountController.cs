using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.ViewModels;

namespace RestaurantMS.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser>   userManager) : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn  = signInManager;
    private readonly UserManager<ApplicationUser>   _users   = userManager;

    // ── Login ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Allow login by username or email
        var user = await _users.FindByNameAsync(model.UserName)
                ?? await _users.FindByEmailAsync(model.UserName);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            ModelState.AddModelError(string.Empty,
                "Invalid credentials or account is inactive.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(
            user, model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var url = model.ReturnUrl;
            if (!string.IsNullOrEmpty(url) && Url.IsLocalUrl(url))
                return Redirect(url);
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Account locked. Try again in 15 minutes.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }

        return View(model);
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Login");
    }

    // ── Access Denied ─────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access Denied";
        return View();
    }

    // ── Profile ───────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        ViewData["Title"] = "My Profile";
        var user = await _users.GetUserAsync(User);
        return View(user);
    }
}
