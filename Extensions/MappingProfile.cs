using AutoMapper;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Extensions;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── PosOrder → PosOrderDto ────────────────────────────────────────────
        CreateMap<PosOrder, PosOrderDto>()
            .ForMember(dst => dst.CashierName, opt => opt.MapFrom(src =>
                src.Cashier != null
                    ? $"{src.Cashier.FullNameAr} / {src.Cashier.FullNameEn}"
                    : string.Empty))
            .ForMember(dst => dst.Items, opt => opt.MapFrom(src => src.OrderItems));

        // ── PosOrderItem → OrderItemDto ───────────────────────────────────────
        CreateMap<PosOrderItem, OrderItemDto>()
            .ForMember(dst => dst.MenuItemNameAr, opt => opt.MapFrom(src =>
                src.MenuItem != null ? src.MenuItem.NameAr : string.Empty))
            .ForMember(dst => dst.MenuItemNameEn, opt => opt.MapFrom(src =>
                src.MenuItem != null ? src.MenuItem.NameEn : string.Empty));

        // ── MenuItem → MenuItemDto ────────────────────────────────────────────
        CreateMap<MenuItem, MenuItemDto>()
            .ForMember(dst => dst.CategoryNameAr, opt => opt.MapFrom(src =>
                src.Category != null ? src.Category.NameAr : string.Empty))
            .ForMember(dst => dst.CategoryNameEn, opt => opt.MapFrom(src =>
                src.Category != null ? src.Category.NameEn : string.Empty));

        // ── CashHandover → CashHandoverDto ────────────────────────────────────
        CreateMap<CashHandover, CashHandoverDto>()
            .ForMember(dst => dst.CashierName, opt => opt.MapFrom(src =>
                src.Cashier != null
                    ? $"{src.Cashier.FullNameAr} / {src.Cashier.FullNameEn}"
                    : string.Empty))
            .ForMember(dst => dst.ManagerName, opt => opt.MapFrom(src =>
                src.Manager != null
                    ? $"{src.Manager.FullNameAr} / {src.Manager.FullNameEn}"
                    : null))
            .ForMember(dst => dst.ExpectedCash, opt => opt.MapFrom(src => src.ExpectedCash));

        // ── EmployeeShift → EmployeeShiftDto ──────────────────────────────────
        CreateMap<EmployeeShift, EmployeeShiftDto>()
            .ForMember(dst => dst.EmployeeNameAr, opt => opt.MapFrom(src =>
                src.Employee != null ? src.Employee.FullNameAr : string.Empty))
            .ForMember(dst => dst.EmployeeNameEn, opt => opt.MapFrom(src =>
                src.Employee != null ? src.Employee.FullNameEn : string.Empty))
            .ForMember(dst => dst.TemplateNameAr, opt => opt.MapFrom(src =>
                src.Template != null ? src.Template.NameAr : string.Empty))
            .ForMember(dst => dst.TemplateNameEn, opt => opt.MapFrom(src =>
                src.Template != null ? src.Template.NameEn : string.Empty));
    }
}
