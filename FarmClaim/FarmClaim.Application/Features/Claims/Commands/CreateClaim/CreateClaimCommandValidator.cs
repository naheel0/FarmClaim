using FluentValidation;

namespace FarmClaim.Application.Features.Claims.Commands.CreateClaim
{
    public class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
    {
        public CreateClaimCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Request.PolicyId)
                .NotEmpty().WithMessage("Policy ID is required");

            RuleFor(x => x.Request.FarmId)
                .NotEmpty().WithMessage("Farm ID is required");

            RuleFor(x => x.Request.IncidentDate)
                .NotEmpty().WithMessage("Incident date is required")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Incident date cannot be in the future");

            RuleFor(x => x.Request.IncidentType)
                .NotEmpty().WithMessage("Incident type is required")
                .MaximumLength(50);

            RuleFor(x => x.Request.Description)
                .MaximumLength(1000)
                .When(x => x.Request.Description != null);

            RuleFor(x => x.Request.DamageDescription)
                .MaximumLength(2000)
                .When(x => x.Request.DamageDescription != null);
        }
    }
}