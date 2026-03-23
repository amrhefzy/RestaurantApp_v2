using FluentValidation;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Validators;

public class AddOrderItemValidator : AbstractValidator<AddOrderItemDto>
{
    public AddOrderItemValidator()
    {
        RuleFor(x => x.MenuItemId)
            .GreaterThan(0)
            .WithMessage("A valid menu item must be selected.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(999)
            .WithMessage("Quantity cannot exceed 999.");

        RuleFor(x => x.Notes)
            .MaximumLength(300)
            .WithMessage("Notes must not exceed 300 characters.")
            .When(x => x.Notes is not null);
    }
}
