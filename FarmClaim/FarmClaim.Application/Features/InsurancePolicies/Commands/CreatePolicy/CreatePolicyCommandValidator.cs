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

            RuleFor(x => x.Request.PolicyNumber)
                .NotEmpty().WithMessage("Policy number is required")
                .MaximumLength(50);

            RuleFor(x => x.Request.Provider)
                .NotEmpty().WithMessage("Provider is required")
                .MaximumLength(200);

            RuleFor(x => x.Request.CropType)
                .NotEmpty().WithMessage("Crop type is required")
                .MaximumLength(100);

            RuleFor(x => x.Request.CoverageAmount)
                .GreaterThan(0);

            RuleFor(x => x.Request.Premium)
                .GreaterThan(0);

            RuleFor(x => x.Request.SumInsured)
                .GreaterThan(0);

            RuleFor(x => x.Request.StartDate)
                .LessThan(x => x.Request.EndDate)
                .WithMessage("Start date must be before end date");
        }
    }
}