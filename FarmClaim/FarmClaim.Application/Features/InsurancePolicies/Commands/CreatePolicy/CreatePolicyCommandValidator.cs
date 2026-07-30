using FluentValidation;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.CreatePolicy
{
    public class CreatePolicyCommandValidator : AbstractValidator<CreatePolicyCommand>
    {
        public CreatePolicyCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Request.FarmId)
                .NotEmpty().WithMessage("Farm ID is required");

            RuleFor(x => x.Request.InsurancePlanId)
                .NotEmpty().WithMessage("Insurance Plan ID is required");

            RuleFor(x => x.Request.StartDate)
                .NotEmpty().WithMessage("Start date is required");

            RuleFor(x => x.Request)
                .Must(x => !x.EndDate.HasValue || x.EndDate.Value > x.StartDate)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.Request.PolicyNumber!)
                .MaximumLength(50)
                .WithMessage("Policy number cannot exceed 50 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Request.PolicyNumber));
        }
    }
}