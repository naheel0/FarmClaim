using FluentValidation;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.UpdatePolicy
{
    public class UpdatePolicyCommandValidator : AbstractValidator<UpdatePolicyCommand>
    {
        public UpdatePolicyCommandValidator()
        {
            RuleFor(x => x.PolicyId)
                .NotEmpty().WithMessage("Policy ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Request.PolicyNumber)
                .MaximumLength(50)
                .When(x => x.Request.PolicyNumber != null);

            RuleFor(x => x.Request.Provider)
                .MaximumLength(200)
                .When(x => x.Request.Provider != null);

            RuleFor(x => x.Request.CoverageAmount)
                .GreaterThan(0)
                .When(x => x.Request.CoverageAmount.HasValue);

            RuleFor(x => x.Request.Premium)
                .GreaterThan(0)
                .When(x => x.Request.Premium.HasValue);

            RuleFor(x => x.Request.SumInsured)
                .GreaterThan(0)
                .When(x => x.Request.SumInsured.HasValue);

            RuleFor(x => x.Request)
                .Must(x => x.StartDate == null || x.EndDate == null || x.StartDate < x.EndDate)
                .WithMessage("Start date must be before end date");
        }
    }
}