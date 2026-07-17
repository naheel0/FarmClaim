using System.Text.Json;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Queries.GetClaimDetail
{
    public class GetClaimDetailQueryHandler : IRequestHandler<GetClaimDetailQuery, AdminClaimDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetClaimDetailQueryHandler> _logger;

        public GetClaimDetailQueryHandler(IApplicationDbContext context, ILogger<GetClaimDetailQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AdminClaimDetailDto> Handle(GetClaimDetailQuery request, CancellationToken ct)
        {
            var claim = await _context.Claims
                .AsNoTracking()
                .Include(c => c.Policy)
                .Include(c => c.Farm).ThenInclude(f => f!.User)
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException(nameof(Claim), request.ClaimId);

            double? aiDamage = null;
            string? aiConfidence = null;
            decimal? suggestedPayout = null;
            string? aiRecommendation = null;

            if (!string.IsNullOrEmpty(claim.AIAnalysisResult))
            {
                try
                {
                    var aiData = JsonDocument.Parse(claim.AIAnalysisResult).RootElement;
                    aiDamage = aiData.TryGetProperty("damagePercentage", out var dp) ? dp.GetDouble() : null;
                    aiConfidence = aiData.TryGetProperty("confidence", out var cf) ? cf.GetString() : null;

                    if (aiDamage.HasValue && claim.Policy != null)
                    {
                        var percentage = (decimal)(aiDamage.Value / 100.0);
                        suggestedPayout = Math.Round(claim.Policy.SumInsured * percentage, 2);

                        aiRecommendation = aiDamage.Value switch
                        {
                            >= 50 => "Approve - Severe damage confirmed by AI",
                            >= 30 => "Approve - Moderate damage confirmed by AI",
                            >= 10 => "Review - Minor damage, verify manually",
                            _ => "Reject - Insufficient damage detected"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse AI analysis for claim {ClaimId}", claim.Id);
                }
            }

            return new AdminClaimDetailDto
            {
                Id = claim.Id,
                PolicyId = claim.PolicyId,
                FarmId = claim.FarmId,
                UserId = claim.UserId,
                FarmerName = claim.Farm?.User != null
                    ? claim.Farm.User.FirstName + " " + claim.Farm.User.LastName
                    : string.Empty,
                FarmerEmail = claim.Farm?.User?.Email ?? string.Empty,
                PolicyNumber = claim.Policy?.PolicyNumber ?? string.Empty,
                FarmName = claim.Farm?.Name ?? string.Empty,
                CropType = claim.Policy?.CropType ?? string.Empty,
                SumInsured = claim.Policy?.SumInsured ?? 0,
                CoverageAmount = claim.Policy?.CoverageAmount ?? 0,
                FarmLatitude = claim.Farm?.Latitude,
                FarmLongitude = claim.Farm?.Longitude,
                IncidentDate = claim.IncidentDate,
                IncidentType = claim.IncidentType,
                Description = claim.Description,
                DamageDescription = claim.DamageDescription,
                Status = claim.Status,
                ApprovedAmount = claim.ApprovedAmount,
                ReviewedBy = claim.ReviewedBy,
                ReviewedAt = claim.ReviewedAt,
                RejectionReason = claim.RejectionReason,
                WeatherSnapshot = claim.WeatherSnapshot,
                AIAnalysisResult = claim.AIAnalysisResult,
                CreatedAt = claim.CreatedAt,
                UpdatedAt = claim.UpdatedAt,
                AIDamagePercentage = aiDamage,
                AIConfidence = aiConfidence,
                SuggestedPayout = suggestedPayout,
                AIRecommendation = aiRecommendation,
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