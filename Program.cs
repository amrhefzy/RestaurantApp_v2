using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using RestaurantMS.Data;
using RestaurantMS.Extensions;
using RestaurantMS.Helpers;
using RestaurantMS.Middleware;
using RestaurantMS.Models.Domain;
using Serilog;

// ── Bootstrap Serilog early (captures startup errors) ──────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    // ── Database ───────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<RestaurantDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(RestaurantDbContext).Assembly.FullName)));

    // ── ASP.NET Core Identity ──────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        // Password policy
        options.Password.RequireDigit           = true;
        options.Password.RequiredLength         = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase       = true;
        options.Password.RequireLowercase       = true;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers      = true;

        // User
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<RestaurantDbContext>()
    .AddDefaultTokenProviders();

    // ── Cookie Authentication ──────────────────────────────────────────────────
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath         = "/Account/Login";
        options.LogoutPath        = "/Account/Logout";
        options.AccessDeniedPath  = "/Account/AccessDenied";
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name       = ".RestaurantMS.Session";
        options.Cookie.HttpOnly   = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // ── Data Protection ────────────────────────────────────────────────────────
    builder.Services.AddDataProtection();

    // ── MVC ────────────────────────────────────────────────────────────────────
    builder.Services.AddControllersWithViews()
        .AddViewLocalization()
        .AddDataAnnotationsLocalization();

    // ── Application Layer (DI) ─────────────────────────────────────────────────
    builder.Services
        .AddRepositories()
        .AddApplicationServices()
        .AddLocalizationServices()
        .AddAutoMapperProfiles()
        .AddFluentValidationSetup();

    // ── Session (for POS hold state, toastr messages) ─────────────────────────
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout        = TimeSpan.FromMinutes(60);
        options.Cookie.HttpOnly    = true;
        options.Cookie.IsEssential = true;
    });

    // ── Anti-forgery ──────────────────────────────────────────────────────────
    builder.Services.AddAntiforgery(options =>
        options.HeaderName = "X-CSRF-TOKEN");

    // ── Memory Cache ──────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();

    // ───────────────────────────────────────────────────────────────────────────
    var app = builder.Build();
    // ───────────────────────────────────────────────────────────────────────────

    // ── Global Exception Handler ───────────────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();

    // ── Environment-specific middleware ────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // ── Static Files (AdminLTE assets in wwwroot) ──────────────────────────────
    app.UseStaticFiles();

    // ── Request Localization (ar / en, cookie-based) ───────────────────────────
    var supportedCultures = new[] { "ar", "ar-SA", "en", "en-US" };
    app.UseRequestLocalization(new RequestLocalizationOptions()
        .SetDefaultCulture("ar")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures));

    // ── Serilog Request Logging ────────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // ── Routing ────────────────────────────────────────────────────────────────
    app.UseRouting();

    // ── Auth ───────────────────────────────────────────────────────────────────
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Session ────────────────────────────────────────────────────────────────
    app.UseSession();

    // ── Routes ─────────────────────────────────────────────────────────────────
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // ── Database initialisation ────────────────────────────────────────────────
    await InitialiseDatabaseAsync(app);

    Log.Information("RestaurantMS started on {Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "RestaurantMS failed to start.");
}
finally
{
    Log.CloseAndFlush();
}

// ── Database seed helper ──────────────────────────────────────────────────────
static async Task InitialiseDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger   = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<RestaurantDbContext>();

        // Auto-migrate in Development; in Production use explicit migration runner
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Applying pending EF migrations…");
            await db.Database.MigrateAsync();
        }

        await SeedRolesAndAdminAsync(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialisation failed.");
        throw;
    }
}

static async Task SeedRolesAndAdminAsync(
    IServiceProvider services,
    ILogger<Program> logger)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var db          = services.GetRequiredService<RestaurantDbContext>();

    // ── Ensure roles exist ────────────────────────────────────────────────────
    string[] roles = ["SuperAdmin", "Manager", "Cashier", "Waiter", "KitchenStaff", "Accountant"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
            logger.LogInformation("Role '{Role}' created.", role);
        }
    }

    // ── Seed default SuperAdmin ───────────────────────────────────────────────
    const string adminEmail    = "admin@restaurant.com";
    const string adminPassword = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName    = "admin",
            Email       = adminEmail,
            FullNameAr  = "مدير النظام",
            FullNameEn  = "System Admin",
            IsActive    = true,
            ManagerPin  = ManagerPinHelper.HashPin("1234"), // default PIN — change on first login
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
            logger.LogInformation(
                "Default SuperAdmin seeded — email: {Email}, password: {Password}, PIN: 1234",
                adminEmail, adminPassword);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to seed admin user: {Errors}", errors);
        }
    }

    // ── Seed sample categories & menu items ───────────────────────────────────
    await SeedMenuAsync(db, logger);
}

static async Task SeedMenuAsync(RestaurantDbContext db, ILogger<Program> logger)
{
    if (db.Categories.Any()) return; // already seeded

    logger.LogInformation("Seeding sample categories and menu items…");

    var cats = new[]
    {
        new Category { NameAr = "المشروبات",   NameEn = "Beverages",   IconClass = "fas fa-coffee",      DisplayOrder = 1 },
        new Category { NameAr = "المقبلات",    NameEn = "Starters",    IconClass = "fas fa-leaf",         DisplayOrder = 2 },
        new Category { NameAr = "الأطباق الرئيسية", NameEn = "Main Course", IconClass = "fas fa-drumstick-bite", DisplayOrder = 3 },
        new Category { NameAr = "البيتزا",     NameEn = "Pizza",       IconClass = "fas fa-pizza-slice",  DisplayOrder = 4 },
        new Category { NameAr = "الحلويات",    NameEn = "Desserts",    IconClass = "fas fa-ice-cream",    DisplayOrder = 5 },
    };
    db.Categories.AddRange(cats);
    await db.SaveChangesAsync();

    var bev  = cats[0]; var sta = cats[1];
    var main = cats[2]; var piz = cats[3]; var des = cats[4];

    var items = new[]
    {
        // Beverages
        new MenuItem { NameAr="قهوة عربية",     NameEn="Arabic Coffee",    Price=5.000m,  CategoryId=bev.Id,  DisplayOrder=1, IsAvailable=true },
        new MenuItem { NameAr="عصير برتقال",    NameEn="Orange Juice",     Price=8.000m,  CategoryId=bev.Id,  DisplayOrder=2, IsAvailable=true },
        new MenuItem { NameAr="شاي بالنعناع",   NameEn="Mint Tea",         Price=4.500m,  CategoryId=bev.Id,  DisplayOrder=3, IsAvailable=true },
        new MenuItem { NameAr="ماء معدني",      NameEn="Mineral Water",    Price=2.000m,  CategoryId=bev.Id,  DisplayOrder=4, IsAvailable=true },
        new MenuItem { NameAr="كابتشينو",       NameEn="Cappuccino",       Price=10.000m, CategoryId=bev.Id,  DisplayOrder=5, IsAvailable=true },

        // Starters
        new MenuItem { NameAr="حمص",            NameEn="Hummus",           Price=12.000m, CategoryId=sta.Id,  DisplayOrder=1, IsAvailable=true },
        new MenuItem { NameAr="فتوش",           NameEn="Fattoush Salad",   Price=14.000m, CategoryId=sta.Id,  DisplayOrder=2, IsAvailable=true },
        new MenuItem { NameAr="سلطة خضراء",    NameEn="Green Salad",      Price=11.000m, CategoryId=sta.Id,  DisplayOrder=3, IsAvailable=true },
        new MenuItem { NameAr="متبل",           NameEn="Mutabbal",         Price=12.000m, CategoryId=sta.Id,  DisplayOrder=4, IsAvailable=true },

        // Main Course
        new MenuItem { NameAr="دجاج مشوي",     NameEn="Grilled Chicken",  Price=35.000m, CategoryId=main.Id, DisplayOrder=1, IsAvailable=true },
        new MenuItem { NameAr="لحم مشوي",      NameEn="Grilled Lamb",     Price=55.000m, CategoryId=main.Id, DisplayOrder=2, IsAvailable=true },
        new MenuItem { NameAr="سمك مشوي",      NameEn="Grilled Fish",     Price=45.000m, CategoryId=main.Id, DisplayOrder=3, IsAvailable=true },
        new MenuItem { NameAr="برغر لحم",      NameEn="Beef Burger",      Price=28.000m, CategoryId=main.Id, DisplayOrder=4, IsAvailable=true },
        new MenuItem { NameAr="شاورما دجاج",   NameEn="Chicken Shawarma", Price=22.000m, CategoryId=main.Id, DisplayOrder=5, IsAvailable=true },

        // Pizza
        new MenuItem { NameAr="بيتزا مرغريتا", NameEn="Margherita Pizza", Price=30.000m, CategoryId=piz.Id,  DisplayOrder=1, IsAvailable=true },
        new MenuItem { NameAr="بيتزا دجاج",    NameEn="Chicken Pizza",    Price=35.000m, CategoryId=piz.Id,  DisplayOrder=2, IsAvailable=true },
        new MenuItem { NameAr="بيتزا خضار",    NameEn="Veggie Pizza",     Price=28.000m, CategoryId=piz.Id,  DisplayOrder=3, IsAvailable=true },

        // Desserts
        new MenuItem { NameAr="أم علي",        NameEn="Om Ali",           Price=18.000m, CategoryId=des.Id,  DisplayOrder=1, IsAvailable=true },
        new MenuItem { NameAr="كنافة",         NameEn="Kunafa",           Price=20.000m, CategoryId=des.Id,  DisplayOrder=2, IsAvailable=true },
        new MenuItem { NameAr="آيس كريم",      NameEn="Ice Cream",        Price=12.000m, CategoryId=des.Id,  DisplayOrder=3, IsAvailable=true },
    };
    db.MenuItems.AddRange(items);
    await db.SaveChangesAsync();

    logger.LogInformation("Seeded {CatCount} categories and {ItemCount} menu items.", cats.Length, items.Length);
}
