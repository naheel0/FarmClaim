using FluentValidation;

namespace FarmClaim.Application.Features.Farms.Commands.CreateFarm;

public class CreateFarmCommandValidator : AbstractValidator<CreateFarmCommand>
{
    public CreateFarmCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Farm name is required")
            .MaximumLength(200).WithMessage("Farm name must not exceed 200 characters");

        RuleFor(x => x.Request.AreaInHectares)
            .GreaterThan(0).WithMessage("Area must be greater than 0");

        RuleFor(x => x.Request.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters")
            .When(x => x.Request.Address != null);

        // GEO: Validate coordinate ranges when provided
        RuleFor(x => x.Request.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Request.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Request.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Request.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180");
    }
}