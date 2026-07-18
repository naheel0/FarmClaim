using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClaimEntity = FarmClaim.Domain.Entities.Claim;

namespace FarmClaim.Application.Features.Admin.Commands.PayClaim
{
    public class PayClaimCommandHandler : IRequestHandler<PayClaimCommand, ClaimResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<PayClaimCommandHandler> _logger;

        public PayClaimCommandHandler(
            IApplicationDbContext context,
            ILogger<PayClaimCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ClaimResponseDto> Handle(PayClaimCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} marking claim {ClaimId} as paid", request.AdminUserId, request.ClaimId);

            var claim = await _context.Claims
                .Include(c => c.Policy)
                .Include(c => c.Farm)
                .Include(c => c.Images)
                .Include(c => c.ReviewedByUser)
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException($"Claim '{request.ClaimId}' not found.");

            if (claim.Status != ClaimStatus.Approved)
                throw new InvalidOperationException(
                    $"Cannot pay. Claim status is '{claim.Status}'. Only 'Approved' claims can be marked as paid.");

            claim.Status = ClaimStatus.Paid;
            claim.PaidAt = DateTime.UtcNow;
            claim.PaymentReference = request.Request.PaymentReference?.Trim();
            claim.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Claim {ClaimId} marked as paid. Ref: {Ref}",
                request.ClaimId, request.Request.PaymentReference ?? "N/A");

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
                ReviewedByUserId = claim.ReviewedByUserId,
                ReviewedByName = claim.ReviewedByUser != null
                    ? $"{claim.ReviewedByUser.FirstName} {claim.ReviewedByUser.LastName}" : null,
                RejectionReason = claim.RejectionReason,
                PaidAt = claim.PaidAt,
                PaymentReference = claim.PaymentReference,
                CreatedAt = claim.CreatedAt,
                UpdatedAt = claim.UpdatedAt,
                Images = claim.Images.Where(i => !i.IsDeleted).Select(i => new ClaimImageDto
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