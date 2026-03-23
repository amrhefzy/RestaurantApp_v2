using FluentValidation;
using RestaurantMS.Models.Domain;
using RestaurantMS.Models.DTOs;

namespace RestaurantMS.Validators;

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentDto>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method.");

        RuleFor(x => x.CashReceived)
            .NotNull()
            .WithMessage("Cash received is required for cash payments.")
            .GreaterThan(0)
            .WithMessage("Cash received must be greater than zero.")
            .When(x => x.PaymentMethod == PaymentMethod.Cash);

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Discount amount cannot be negative.")
            .When(x => x.DiscountAmount.HasValue);
    }
}
