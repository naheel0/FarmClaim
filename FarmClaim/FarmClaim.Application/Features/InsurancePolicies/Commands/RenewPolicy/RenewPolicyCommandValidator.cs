using FluentValidation;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.RenewPolicy
{
    public class RenewPolicyCommandValidator : AbstractValidator<RenewPolicyCommand>
    {
        public RenewPolicyCommandValidator()
        {
            RuleFor(x => x.PolicyId)
                .NotEmpty().WithMessage("Policy ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.MinValue)
                .When(x => x.StartDate.HasValue)
                .WithMessage("Start date must be a valid date");
        }
    }
}
