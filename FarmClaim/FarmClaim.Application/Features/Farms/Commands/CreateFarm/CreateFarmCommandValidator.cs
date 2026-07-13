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
    }
}