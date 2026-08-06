using FluentValidation;
using FarmClaim.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.Claims.Commands.CreateClaim
{
    public class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
    {
        private readonly IApplicationDbContext _context;

        public CreateClaimCommandValidator(IApplicationDbContext context)
        {
            _context = context;

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
                .IsInEnum().WithMessage("Invalid incident type");

            RuleFor(x => x.Request.Description)
                .MaximumLength(1000)
                .When(x => x.Request.Description != null);

            RuleFor(x => x.Request.DamageDescription)
                .MaximumLength(2000)
                .When(x => x.Request.DamageDescription != null);

            // PROD: A claim requires a geo-tagged farm for weather verification.
            RuleFor(x => x.Request.FarmId)
                .MustAsync(HasFarmCoordinates)
                .WithMessage("This farm has no location set. Add a location to the farm before filing a claim so weather verification can run.");
        }

        private async Task<bool> HasFarmCoordinates(Guid farmId, CancellationToken ct)
        {
            var farm = await _context.Farms
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == farmId && !f.IsDeleted, ct);

            return farm != null && farm.Latitude.HasValue && farm.Longitude.HasValue;
        }
    }
}