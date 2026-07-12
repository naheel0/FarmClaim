using FluentValidation;
using FarmClaim.Application.Features.Farms.DTOs;

namespace FarmClaim.Application.Features.Farms.Commands.UpdateFarm
{
    public class UpdateFarmCommandValidator : AbstractValidator<UpdateFarmCommand>
    {
        public UpdateFarmCommandValidator()
        {
            RuleFor(x => x.FarmId)
                .NotEmpty().WithMessage("Farm ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Request.Name)
                .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Request.Name))
                .Matches(@"^[a-zA-Z0-9\s'\-,\.]+$").When(x => !string.IsNullOrEmpty(x.Request.Name));

            RuleFor(x => x.Request.AreaInHectares)
                .GreaterThan(0).When(x => x.Request.AreaInHectares.HasValue)
                .LessThanOrEqualTo(1000000).When(x => x.Request.AreaInHectares.HasValue);
        }
    }
}