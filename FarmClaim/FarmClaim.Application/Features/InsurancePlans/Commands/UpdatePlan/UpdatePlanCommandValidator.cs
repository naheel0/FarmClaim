using FluentValidation;

namespace FarmClaim.Application.Features.InsurancePlans.Commands.UpdatePlan
{
    public class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
    {
        public UpdatePlanCommandValidator()
        {
            RuleFor(x => x.PlanId).NotEmpty();
            RuleFor(x => x.AdminUserId).NotEmpty();

            RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Request.CropType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.Provider).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Request.PremiumRatePerHectare).GreaterThan(0);
            RuleFor(x => x.Request.SumInsuredPerHectare).GreaterThan(0);
            RuleFor(x => x.Request.CoveragePercentage).InclusiveBetween(1, 100);
            RuleFor(x => x.Request.PolicyDurationMonths).InclusiveBetween(1, 60);

            RuleFor(x => x.Request)
                .Must(x => !x.MinAreaInHectares.HasValue || !x.MaxAreaInHectares.HasValue
                           || x.MinAreaInHectares <= x.MaxAreaInHectares)
                .WithMessage("MinAreaInHectares cannot exceed MaxAreaInHectares");
        }
    }
}