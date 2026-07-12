using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.CreateClaim
{
    public class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, ClaimResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CreateClaimCommandHandler> _logger;

        public CreateClaimCommandHandler(
            IApplicationDbContext context,
            ILogger<CreateClaimCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClaimResponseDto> Handle(CreateClaimCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Creating claim for user: {UserId}, Policy: {PolicyId}", command.UserId, command.Request.PolicyId);

            // Verify policy exists, belongs to user, and is active
            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .FirstOrDefaultAsync(p => p.Id == command.Request.PolicyId
                    && p.Farm!.UserId == command.UserId
                    && p.IsActive
                    && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException(nameof(InsurancePolicy), command.Request.PolicyId);

            // Verify farm exists and belongs to user
            var farmExists = await _context.Farms
                .AnyAsync(f => f.Id == command.Request.FarmId
                    && f.UserId == command.UserId
                    && !f.IsDeleted, ct);

            if (!farmExists)
                throw new NotFoundException(nameof(Farm), command.Request.FarmId);

            // Verify policy belongs to the farm
            if (policy.FarmId != command.Request.FarmId)
                throw new ValidationException(new List<string> { "The selected policy does not belong to the selected farm" });

            // Verify incident date is within policy period
            if (command.Request.IncidentDate < policy.StartDate || command.Request.IncidentDate > policy.EndDate)
                throw new ValidationException(new List<string> { $"Incident date must be within the policy period ({policy.StartDate:yyyy-MM-dd} to {policy.EndDate:yyyy-MM-dd})" });

            var claim = new Claim
            {
                PolicyId = command.Request.PolicyId,
                FarmId = command.Request.FarmId,
                UserId = command.UserId,
                IncidentDate = command.Request.IncidentDate,
                IncidentType = command.Request.IncidentType.Trim(),
                Description = command.Request.Description?.Trim(),
                DamageDescription = command.Request.DamageDescription?.Trim(),
                Status = "Pending"
            };

            await _context.Claims.AddAsync(claim, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Claim created: {ClaimId} for Policy: {PolicyId}", claim.Id, claim.PolicyId);

            return new ClaimResponseDto
            {
                Id = claim.Id,
                PolicyId = claim.PolicyId,
                FarmId = claim.FarmId,
                UserId = claim.UserId,
                PolicyNumber = policy.PolicyNumber,
                FarmName = policy.Farm.Name,
                IncidentDate = claim.IncidentDate,
                IncidentType = claim.IncidentType,
                Description = claim.Description,
                DamageDescription = claim.DamageDescription,
                Status = claim.Status,
                ApprovedAmount = claim.ApprovedAmount,
                ReviewedBy = claim.ReviewedBy,
                ReviewedAt = claim.ReviewedAt,
                RejectionReason = claim.RejectionReason,
                CreatedAt = claim.CreatedAt,
                UpdatedAt = claim.UpdatedAt,
                Images = new()
            };
        }
    }
}