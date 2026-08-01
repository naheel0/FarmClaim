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
            string? aiDamageDescription = null;
            string? aiConfidence = null;
            decimal? suggestedPayout = null;
            string? aiRecommendation = null;

            if (!string.IsNullOrEmpty(claim.AIAnalysisResult))
            {
                try
                {
                    var aiData = JsonDocument.Parse(claim.AIAnalysisResult).RootElement;

                    // Skip if result contains an error (e.g. "AI analysis unavailable")
                    if (aiData.TryGetProperty("error", out _))
                    {
                        _logger.LogInformation("Claim {ClaimId} has AI error result, skipping", claim.Id);
                    }
                    else
                    {
                        aiDamage = aiData.TryGetProperty("damagePercentage", out var dp) ? dp.GetDouble() : null;
                        aiDamageDescription = aiData.TryGetProperty("damageDescription", out var dd) ? dd.GetString() : null;
                        aiConfidence = aiData.TryGetProperty("confidence", out var cf) ? cf.GetString() : null;

                        if (aiDamage.HasValue && claim.Policy != null)
                        {
                            // H4: use CoverageAmount (already factors in CoveragePercentage and area)
                            // SuggestedPayout = CoverageAmount × (damagePercentage / 100)
                            var damageFraction = (decimal)(aiDamage.Value / 100.0);
                            suggestedPayout = Math.Round(claim.Policy.CoverageAmount * damageFraction, 2);

                            // M1: factor confidence — "Low" confidence downgrades recommendation
                            var confidence = aiConfidence ?? "Low";
                            aiRecommendation = (aiDamage.Value, confidence) switch
                            {
                                (>= 50, "High") => "Approve - Severe damage confirmed by AI (high confidence)",
                                (>= 50, _)    => "Review - Severe damage indicated (low confidence, verify)",
                                (>= 30, "High") => "Approve - Moderate damage confirmed by AI (high confidence)",
                                (>= 30, _)    => "Review - Moderate damage indicated (low confidence, verify)",
                                (>= 10, _)    => "Review - Minor damage, verify manually",
                                _             => "Reject - Insufficient damage detected"
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse AI analysis for claim {ClaimId}", claim.Id);
                }
            }

            double? weatherTemp = null;
            double? weatherRain = null;
            double? weatherWind = null;
            string? weatherCondition = null;
            string? weatherSource = null;
            DateTime? weatherDate = null;

            if (!string.IsNullOrEmpty(claim.WeatherSnapshot))
            {
                try
                {
                    var weatherData = JsonDocument.Parse(claim.WeatherSnapshot).RootElement;
                    weatherTemp = weatherData.TryGetProperty("temperatureCelsius", out var t) ? t.GetDouble() : null;
                    weatherRain = weatherData.TryGetProperty("rainfallMm", out var r) ? r.GetDouble() : null;
                    weatherWind = weatherData.TryGetProperty("windSpeedKmh", out var w) ? w.GetDouble() : null;
                    weatherCondition = weatherData.TryGetProperty("weatherCondition", out var wc) ? wc.GetString() : null;
                    weatherSource = weatherData.TryGetProperty("source", out var src) ? src.GetString() : null;
                    if (weatherData.TryGetProperty("date", out var dt) && dt.TryGetDateTime(out var parsed))
                        weatherDate = parsed;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse weather snapshot for claim {ClaimId}", claim.Id);
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
                AIDamageDescription = aiDamageDescription,
                AIConfidence = aiConfidence,
                SuggestedPayout = suggestedPayout,
                AIRecommendation = aiRecommendation,
                WeatherTemperatureCelsius = weatherTemp,
                WeatherRainfallMm = weatherRain,
                WeatherWindSpeedKmh = weatherWind,
                WeatherCondition = weatherCondition,
                WeatherSource = weatherSource,
                WeatherDate = weatherDate,
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