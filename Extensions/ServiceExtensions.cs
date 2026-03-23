using FluentValidation;
using RestaurantMS.Models.Domain;
using RestaurantMS.Repositories;
using RestaurantMS.Repositories.Interfaces;
using RestaurantMS.Services;
using RestaurantMS.Services.Interfaces;
using RestaurantMS.Validators;

namespace RestaurantMS.Extensions;

public static class ServiceExtensions
{
    // ── Repositories + Unit of Work ───────────────────────────────────────────
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Specific repositories (also accessible through UoW, but registered for
        // any controller/service that needs direct injection)
        services.AddScoped<IOrderRepository,        OrderRepository>();
        services.AddScoped<IMenuRepository,         MenuRepository>();
        services.AddScoped<ICashHandoverRepository, CashHandoverRepository>();
        services.AddScoped<IShiftRepository,        ShiftRepository>();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        return services;
    }

    // ── Application Services ──────────────────────────────────────────────────
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPosService,      PosService>();
        services.AddScoped<IHandoverService, HandoverService>();
        services.AddScoped<IShiftService,    ShiftService>();
        services.AddScoped<IReportService,   ReportService>();
        services.AddScoped<IPrintService,    PrintService>();

        return services;
    }

    // ── Localization ──────────────────────────────────────────────────────────
    public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ILocalizationService, LocalizationService>();

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supported = new[] { "ar", "ar-SA", "en", "en-US" };
            options.SetDefaultCulture("ar")
                   .AddSupportedCultures(supported)
                   .AddSupportedUICultures(supported);
            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        return services;
    }

    // ── AutoMapper ────────────────────────────────────────────────────────────
    public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }

    // ── FluentValidation ──────────────────────────────────────────────────────
    public static IServiceCollection AddFluentValidationSetup(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PosOrderValidator>();
        return services;
    }
}
