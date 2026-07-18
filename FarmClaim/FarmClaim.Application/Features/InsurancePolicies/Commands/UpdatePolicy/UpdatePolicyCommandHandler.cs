using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.InsurancePolicies.Commands.UpdatePolicy
{
    public class UpdatePolicyCommandHandler : IRequestHandler<UpdatePolicyCommand, PolicyResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdatePolicyCommandHandler> _logger;

        public UpdatePolicyCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdatePolicyCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PolicyResponseDto> Handle(UpdatePolicyCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Updating policy: {PolicyId} for user: {UserId}", command.PolicyId, command.UserId);

            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .Include(p => p.Claims)
                .FirstOrDefaultAsync(p => p.Id == command.PolicyId
                    && p.Farm!.UserId == command.UserId
                    && !p.IsDeleted, ct);

            if (policy == null)
            {
                _logger.LogWarning("Policy not found: {PolicyId} or not owned by user: {UserId}", command.PolicyId, command.UserId);
                throw new NotFoundException(nameof(InsurancePolicy), command.PolicyId);
            }

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(command.Request.PolicyNumber))
            {
                var duplicate = await _context.InsurancePolicies
                    .AnyAsync(p => p.PolicyNumber == command.Request.PolicyNumber.Trim()
                        && p.Id != command.PolicyId && !p.IsDeleted, ct);

                if (duplicate)
                    throw new ValidationException(new List<string> { $"A policy with number '{command.Request.PolicyNumber}' already exists" });

                policy.PolicyNumber = command.Request.PolicyNumber.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Request.Provider))
            {
                policy.Provider = command.Request.Provider.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Request.CropType))
            {
                policy.CropType = command.Request.CropType.Trim();
                hasChanges = true;
            }

            if (command.Request.CoverageAmount.HasValue)
            {
                policy.CoverageAmount = command.Request.CoverageAmount.Value;
                hasChanges = true;
            }

            if (command.Request.Premium.HasValue)
            {
                policy.Premium = command.Request.Premium.Value;
                hasChanges = true;
            }

            if (command.Request.SumInsured.HasValue)
            {
                policy.SumInsured = command.Request.SumInsured.Value;
                hasChanges = true;
            }

            if (command.Request.StartDate.HasValue)
            {
                policy.StartDate = command.Request.StartDate.Value;
                hasChanges = true;
            }

            if (command.Request.EndDate.HasValue)
            {
                policy.EndDate = command.Request.EndDate.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                policy.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Policy updated: {PolicyId}", policy.Id);
            }
            else
            {
                _logger.LogInformation("No changes detected for policy: {PolicyId}", policy.Id);
            }

            return new PolicyResponseDto
            {
                Id = policy.Id,
                FarmId = policy.FarmId,
                UserId = policy.Farm!.UserId,
                FarmName = policy.Farm.Name,
                PolicyNumber = policy.PolicyNumber,
                Provider = policy.Provider,
                CropType = policy.CropType,
                CoverageAmount = policy.CoverageAmount,
                Premium = policy.Premium,
                SumInsured = policy.SumInsured,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                Status = policy.Status,
                ApprovedAt = policy.ApprovedAt,
                ApprovedByUserId = policy.ApprovedByUserId,
                RejectedAt = policy.RejectedAt,
                RejectionReason = policy.RejectionReason,
                CancelledAt = policy.CancelledAt,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
                ClaimsCount = policy.Claims.Count(c => !c.IsDeleted)
            };
        }
    }
}