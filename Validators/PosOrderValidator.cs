using FluentValidation;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Validators;

public class PosOrderValidator : AbstractValidator<CreateOrderDto>
{
    public PosOrderValidator()
    {
        RuleFor(x => x.OrderType)
            .IsInEnum()
            .WithMessage("Invalid order type.");

        RuleFor(x => x.TableNumber)
            .MaximumLength(20)
            .WithMessage("Table number must not exceed 20 characters.")
            .When(x => x.TableNumber is not null);

        RuleFor(x => x.TableNumber)
            .NotEmpty()
            .WithMessage("Table number is required for dine-in orders.")
            .When(x => x.OrderType == OrderType.DineIn);
    }
}
