using FluentValidation;

namespace FarmClaim.Application.Features.Claims.Commands.UpdateClaim
{
    public class UpdateClaimCommandValidator : AbstractValidator<UpdateClaimCommand>
    {
        public UpdateClaimCommandValidator()
        {
            RuleFor(x => x.ClaimId)
                .NotEmpty().WithMessage("Claim ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Request.IncidentType)
                .MaximumLength(50)
                .When(x => x.Request.IncidentType != null);

            RuleFor(x => x.Request.Description)
                .MaximumLength(1000)
                .When(x => x.Request.Description != null);

            RuleFor(x => x.Request.DamageDescription)
                .MaximumLength(2000)
                .When(x => x.Request.DamageDescription != null);

            RuleFor(x => x.Request.IncidentDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.Request.IncidentDate.HasValue)
                .WithMessage("Incident date cannot be in the future");
        }
    }
}