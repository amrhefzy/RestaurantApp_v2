using FluentValidation;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Validators;

public class OpenHandoverValidator : AbstractValidator<OpenHandoverDto>
{
    public OpenHandoverValidator()
    {
        RuleFor(x => x.OpenFloat)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Opening float cannot be negative.")
            .LessThanOrEqualTo(100_000)
            .WithMessage("Opening float cannot exceed 100,000.");
    }
}
