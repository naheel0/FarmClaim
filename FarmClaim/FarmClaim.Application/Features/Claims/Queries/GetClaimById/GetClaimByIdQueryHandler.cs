using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Queries.GetClaimById
{
    public class GetClaimByIdQueryHandler : IRequestHandler<GetClaimByIdQuery, ClaimResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetClaimByIdQueryHandler> _logger;

        public GetClaimByIdQueryHandler(
            IApplicationDbContext context,
            ILogger<GetClaimByIdQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClaimResponseDto> Handle(GetClaimByIdQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Getting claim {ClaimId} for user {UserId}", request.ClaimId, request.UserId);

            var claim = await _context.Claims
                .AsNoTracking()
                .Include(c => c.Policy)
                .Include(c => c.Farm)
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId
                    && c.UserId == request.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
            {
                _logger.LogWarning("Claim not found: {ClaimId} or not owned by user: {UserId}", request.ClaimId, request.UserId);
                throw new NotFoundException(nameof(Claim), request.ClaimId);
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