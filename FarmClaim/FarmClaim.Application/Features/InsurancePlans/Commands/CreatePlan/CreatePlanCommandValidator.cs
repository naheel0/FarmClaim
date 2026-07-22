using FluentValidation;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.CreatePlan
{
    public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
    {
        public CreatePlanCommandValidator()
        {
            RuleFor(x => x.AdminUserId).NotEmpty();

            RuleFor(x => x.Request.Name)
                .NotEmpty().WithMessage("Plan name is required")
                .MaximumLength(200);

            RuleFor(x => x.Request.CropType)
                .NotEmpty().WithMessage("Crop type is required")
                .MaximumLength(100);

            RuleFor(x => x.Request.Provider)
                .NotEmpty().WithMessage("Provider is required")
                .MaximumLength(200);

            RuleFor(x => x.Request.PremiumRatePerHectare).GreaterThan(0);
            RuleFor(x => x.Request.SumInsuredPerHectare).GreaterThan(0);

            RuleFor(x => x.Request.CoveragePercentage)
                .InclusiveBetween(1, 100)
                .WithMessage("Coverage percentage must be between 1 and 100");

            RuleFor(x => x.Request.PolicyDurationMonths)
                .InclusiveBetween(1, 60)
                .WithMessage("Policy duration must be between 1 and 60 months");

            RuleFor(x => x.Request)
                .Must(x => !x.MinAreaInHectares.HasValue || !x.MaxAreaInHectares.HasValue
                           || x.MinAreaInHectares <= x.MaxAreaInHectares)
                .WithMessage("MinAreaInHectares cannot exceed MaxAreaInHectares");
        }
    }
}