using FluentValidation;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Validators;

public class CloseHandoverValidator : AbstractValidator<CloseHandoverDto>
{
    public CloseHandoverValidator()
    {
        RuleFor(x => x.HandoverId)
            .GreaterThan(0)
            .WithMessage("A valid handover ID is required.");

        RuleFor(x => x.ActualCash)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Actual cash cannot be negative.")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Actual cash cannot exceed 1,000,000.");
    }
}
