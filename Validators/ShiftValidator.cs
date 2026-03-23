using FluentValidation;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Validators;

public class ShiftValidator : AbstractValidator<AssignShiftDto>
{
    public ShiftValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage("A valid employee must be selected.");

        RuleFor(x => x.TemplateId)
            .GreaterThan(0)
            .WithMessage("A valid shift template must be selected.");

        RuleFor(x => x.ShiftDate)
            .NotEmpty()
            .WithMessage("Shift date is required.")
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Shift date cannot be in the past.");
    }
}
