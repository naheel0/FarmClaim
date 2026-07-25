using FluentValidation;

namespace FarmClaim.Application.Features.Payments.Commands.CreateOrder
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();

            When(x => x.Request.CustomAmount.HasValue, () =>
            {
                RuleFor(x => x.Request.CustomAmount!.Value)
                    .GreaterThan(0).WithMessage("Custom amount must be greater than 0");
            });
        }
    }
}