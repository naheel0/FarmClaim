using System.Text.Json;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.CreateClaim
{
    public class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, ClaimResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IWeatherService _weatherService;
        private readonly IClaimBackgroundJobService _backgroundJobService;
        private readonly ILogger<CreateClaimCommandHandler> _logger;

        public CreateClaimCommandHandler(
            IApplicationDbContext context,
            IWeatherService weatherService,
            IClaimBackgroundJobService backgroundJobService,
            ILogger<CreateClaimCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClaimResponseDto> Handle(CreateClaimCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Creating claim for user: {UserId}, Policy: {PolicyId}",
                command.UserId, command.Request.PolicyId);

            // Validate policy: belongs to user, is Active, not expired
            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .FirstOrDefaultAsync(p => p.Id == command.Request.PolicyId
                    && p.Farm!.UserId == command.UserId
                    && p.Status == PolicyStatus.Active
                    && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException(nameof(InsurancePolicy), command.Request.PolicyId);

            // NEW: Check policy not expired
            if (policy.EndDate < DateTime.UtcNow)
                throw new ValidationException(new List<string>
                {
                    $"This policy expired on {policy.EndDate:yyyy-MM-dd}. Cannot file a claim."
                });

            // Validate farm belongs to user
            var farm = await _context.Farms
                .FirstOrDefaultAsync(f => f.Id == command.Request.FarmId
                    && f.UserId == command.UserId
                    && !f.IsDeleted, ct);

            if (farm == null)
                throw new NotFoundException(nameof(Farm), command.Request.FarmId);

            // Validate policy belongs to the selected farm
            if (policy.FarmId != command.Request.FarmId)
                throw new ValidationException(new List<string>
                {
                    "The selected policy does not belong to the selected farm"
                });

            // Validate incident date is within policy period
            if (command.Request.IncidentDate < policy.StartDate
                || command.Request.IncidentDate > policy.EndDate)
                throw new ValidationException(new List<string>
                {
                    $"Incident date must be within the policy period ({policy.StartDate:yyyy-MM-dd} to {policy.EndDate:yyyy-MM-dd})"
                });

            // NEW: Check for duplicate claim on same policy + same incident date
            var duplicate = await _context.Claims
                .AnyAsync(c => c.PolicyId == command.Request.PolicyId
                    && c.FarmId == command.Request.FarmId
                    && c.IncidentDate.Date == command.Request.IncidentDate.Date
                    && !c.IsDeleted, ct);

            if (duplicate)
                throw new ValidationException(new List<string>
                {
                    "A claim already exists for this policy and incident date."
                });

            var claim = new Claim
            {
                PolicyId = command.Request.PolicyId,
                FarmId = command.Request.FarmId,
                UserId = command.UserId,
                IncidentDate = command.Request.IncidentDate,
                IncidentType = command.Request.IncidentType,
                Description = command.Request.Description?.Trim(),
                DamageDescription = command.Request.DamageDescription?.Trim(),
                Status = ClaimStatus.Pending
            };

            // Weather API (your existing code — unchanged)
            try
            {
                if (farm.Latitude.HasValue && farm.Longitude.HasValue)
                {
                    var weather = await _weatherService.GetWeatherAsync(
                        farm.Latitude.Value, farm.Longitude.Value,
                        command.Request.IncidentDate, ct);

                    claim.WeatherSnapshot = JsonSerializer.Serialize(weather,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    _logger.LogInformation("Weather snapshot saved for claim");
                }
                else
                {
                    _logger.LogWarning("Farm {FarmId} has no coordinates, skipping weather fetch", farm.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather API failed, continuing without weather data");
                claim.WeatherSnapshot = JsonSerializer.Serialize(new
                {
                    error = "Weather data unavailable",
                    message = ex.Message
                });
            }

            // Store image URLs as ClaimImage entities so the background job can fetch them
            var initialImageUrls = command.Request.ImageUrls ?? new List<string>();
            if (initialImageUrls.Count > 0)
            {
                for (int i = 0; i < initialImageUrls.Count; i++)
                {
                    var claimImage = new ClaimImage
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claim.Id,
                        ImageUrl = initialImageUrls[i],
                        DisplayOrder = i,
                        IsPrimary = i == 0
                    };
                    claim.Images.Add(claimImage);
                }
            }

            await _context.Claims.AddAsync(claim, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Claim created: {ClaimId} for Policy: {PolicyId}",
                claim.Id, claim.PolicyId);

            // Enqueue AI analysis as a background job (non-blocking)
            if (initialImageUrls.Count > 0)
            {
                _backgroundJobService.EnqueueAIAnalysis(claim.Id);
                _logger.LogInformation("AI analysis enqueued for claim {ClaimId}", claim.Id);
            }

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
                WeatherSnapshot = claim.WeatherSnapshot,
                AIAnalysisResult = claim.AIAnalysisResult,
                CreatedAt = claim.CreatedAt,
                UpdatedAt = claim.UpdatedAt,
                Images = new()
            };
        }
    }
}