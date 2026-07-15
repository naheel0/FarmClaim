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
        private readonly IGeminiVisionService _geminiService;
        private readonly ILogger<CreateClaimCommandHandler> _logger;

        public CreateClaimCommandHandler(
            IApplicationDbContext context,
            IWeatherService weatherService,
            IGeminiVisionService geminiService,
            ILogger<CreateClaimCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClaimResponseDto> Handle(CreateClaimCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Creating claim for user: {UserId}, Policy: {PolicyId}", command.UserId, command.Request.PolicyId);

            var policy = await _context.InsurancePolicies
                .Include(p => p.Farm)
                .FirstOrDefaultAsync(p => p.Id == command.Request.PolicyId
                    && p.Farm!.UserId == command.UserId
                    && p.IsActive
                    && !p.IsDeleted, ct);

            if (policy == null)
                throw new NotFoundException(nameof(InsurancePolicy), command.Request.PolicyId);

            var farm = await _context.Farms
                .FirstOrDefaultAsync(f => f.Id == command.Request.FarmId
                    && f.UserId == command.UserId
                    && !f.IsDeleted, ct);

            if (farm == null)
                throw new NotFoundException(nameof(Farm), command.Request.FarmId);

            if (policy.FarmId != command.Request.FarmId)
                throw new ValidationException(new List<string> { "The selected policy does not belong to the selected farm" });

            if (command.Request.IncidentDate < policy.StartDate || command.Request.IncidentDate > policy.EndDate)
                throw new ValidationException(new List<string> { $"Incident date must be within the policy period ({policy.StartDate:yyyy-MM-dd} to {policy.EndDate:yyyy-MM-dd})" });

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

            // Weather API
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

            // Gemini Vision
            try
            {
                var imageUrls = command.Request.ImageUrls ?? new List<string>();
                if (imageUrls.Count > 0)
                {
                    var analysis = await _geminiService.AnalyzeImagesAsync(imageUrls, policy.CropType, ct);

                    claim.AIAnalysisResult = JsonSerializer.Serialize(analysis,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    _logger.LogInformation("AI analysis saved: {Damage}%", analysis.DamagePercentage);
                }
                else
                {
                    _logger.LogInformation("No images provided, skipping AI analysis");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini Vision failed, continuing without AI analysis");
                claim.AIAnalysisResult = JsonSerializer.Serialize(new
                {
                    error = "AI analysis unavailable",
                    message = ex.Message
                });
            }

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
                WeatherSnapshot = claim.WeatherSnapshot,
                AIAnalysisResult = claim.AIAnalysisResult,
                CreatedAt = claim.CreatedAt,
                UpdatedAt = claim.UpdatedAt,
                Images = new()
            };
        }
    }
}