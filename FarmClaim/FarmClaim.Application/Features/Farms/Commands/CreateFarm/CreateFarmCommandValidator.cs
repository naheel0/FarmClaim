using FluentValidation;
using FarmClaim.Application.Features.Farms.DTOs;

namespace FarmClaim.Application.Features.Farms.Commands.CreateFarm
{
    public class CreateFarmCommandValidator : AbstractValidator<CreateFarmCommand>
    {
        public CreateFarmCommandValidator()
        {
            RuleFor(x => x.Request.Name)
                .NotEmpty().WithMessage("Farm name is required")
                .MaximumLength(200).WithMessage("Name too long")
                .Matches(@"^[a-zA-Z0-9\s'\-,\.]+$").WithMessage("Invalid characters in name");

            RuleFor(x => x.Request.AreaInHectares)
                .GreaterThan(0).WithMessage("Area must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Area cannot exceed 1,000,000 hectares");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Request.Address)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Request.Address));
        }
    }
}