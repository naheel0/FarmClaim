using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.UpdateClaim
{
    public class UpdateClaimCommandHandler : IRequestHandler<UpdateClaimCommand, ClaimResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdateClaimCommandHandler> _logger;

        public UpdateClaimCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdateClaimCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClaimResponseDto> Handle(UpdateClaimCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Updating claim: {ClaimId} for user: {UserId}", command.ClaimId, command.UserId);

            var claim = await _context.Claims
                .Include(c => c.Policy)
                .Include(c => c.Farm)
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == command.ClaimId
                    && c.UserId == command.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
            {
                _logger.LogWarning("Claim not found: {ClaimId} or not owned by user: {UserId}", command.ClaimId, command.UserId);
                throw new NotFoundException(nameof(Claim), command.ClaimId);
            }

            // Only allow updates if claim is still Pending
            if (claim.Status != "Pending")
                throw new ValidationException(new List<string> { $"Cannot update claim with status '{claim.Status}'. Only pending claims can be updated." });

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(command.Request.IncidentType))
            {
                claim.IncidentType = command.Request.IncidentType.Trim();
                hasChanges = true;
            }

            if (command.Request.Description != null)
            {
                claim.Description = command.Request.Description.Trim();
                hasChanges = true;
            }

            if (command.Request.DamageDescription != null)
            {
                claim.DamageDescription = command.Request.DamageDescription.Trim();
                hasChanges = true;
            }

            if (command.Request.IncidentDate.HasValue)
            {
                // Verify new incident date is within policy period
                if (claim.Policy != null)
                {
                    if (command.Request.IncidentDate.Value < claim.Policy.StartDate
                        || command.Request.IncidentDate.Value > claim.Policy.EndDate)
                        throw new ValidationException(new List<string> { $"Incident date must be within the policy period" });
                }
                claim.IncidentDate = command.Request.IncidentDate.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                claim.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Claim updated: {ClaimId}", claim.Id);
            }
            else
            {
                _logger.LogInformation("No changes detected for claim: {ClaimId}", claim.Id);
            }

            return new ClaimResponseDto
            {
                Id = claim.Id,
                PolicyId = claim.PolicyId,
                FarmId = claim.FarmId,
                UserId = claim.UserId,
                PolicyNumber = claim.Policy?.PolicyNumber ?? string.Empty,
                FarmName = claim.Farm?.Name ?? string.Empty,
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
                Images = claim.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ClaimImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        ThumbnailUrl = i.ThumbnailUrl,
                        FileName = i.FileName,
                        FileType = i.FileType,
                        FileSizeBytes = i.FileSizeBytes,
                        DisplayOrder = i.DisplayOrder,
                        IsPrimary = i.IsPrimary
                    }).ToList()
            };
        }
    }
}