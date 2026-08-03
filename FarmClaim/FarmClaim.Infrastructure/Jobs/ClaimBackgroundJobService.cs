using System.Text.Json;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Notifications.DTOs;
using FarmClaim.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Jobs
{
    public class ClaimBackgroundJobService
    {
        private readonly IApplicationDbContext _context;
        private readonly IWeatherService _weatherService;
        private readonly IGeminiVisionService _geminiService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ClaimBackgroundJobService> _logger;

        public ClaimBackgroundJobService(
            IApplicationDbContext context,
            IWeatherService weatherService,
            IGeminiVisionService geminiService,
            INotificationService notificationService,
            ILogger<ClaimBackgroundJobService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================
        // WEATHER ANALYSIS
        // ============================================
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 300 })]
        public async Task ProcessWeatherAnalysisAsync(Guid claimId)
        {
            _logger.LogInformation("Hangfire: Starting weather analysis for claim {ClaimId}", claimId);

            var claim = await _context.Claims
                .Include(c => c.Policy).ThenInclude(p => p!.Farm)
                .FirstOrDefaultAsync(c => c.Id == claimId && !c.IsDeleted);

            if (claim == null)
            {
                _logger.LogWarning("Hangfire: Claim {ClaimId} not found", claimId);
                return;
            }

            if (claim.WeatherSnapshot != null)
            {
                _logger.LogInformation("Hangfire: Claim {ClaimId} already has weather data, skipping", claimId);
                return;
            }

            if (claim.Policy?.Farm?.Latitude == null || claim.Policy.Farm.Longitude == null)
            {
                _logger.LogWarning("Hangfire: Farm has no coordinates for claim {ClaimId}", claimId);
                return;
            }

            try
            {
                var weather = await _weatherService.GetWeatherAsync(
                    claim.Policy.Farm.Latitude.Value,
                    claim.Policy.Farm.Longitude.Value,
                    claim.IncidentDate);

                claim.WeatherSnapshot = JsonSerializer.Serialize(weather, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                claim.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Hangfire: Weather done for claim {ClaimId}. Temp={Temp}C, Rain={Rain}mm",
                    claimId, weather.TemperatureCelsius, weather.RainfallMm);

                // Notify farmer
                await _notificationService.SendClaimUpdateAsync(claim.UserId, new ClaimNotificationDto
                {
                    ClaimId = claimId,
                    Status = claim.Status,
                    Title = "Weather Data Retrieved",
                    Message = $"Weather on {claim.IncidentDate:yyyy-MM-dd}: {weather.WeatherCondition}, " +
                              $"{weather.TemperatureCelsius}°C, Rainfall: {weather.RainfallMm}mm",
                    NotificationType = "WeatherComplete"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hangfire: Weather analysis failed for claim {ClaimId}", claimId);
                throw;
            }
        }

        // ============================================
        // AI ANALYSIS
        // ============================================
        // H5: Prevent concurrent execution — SlidingInvisibilityTimeout is 5min,
        // Gemini can take longer. Without this, job could run twice simultaneously.
        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 300 })]
        public async Task ProcessAIAnalysisAsync(Guid claimId)
        {
            _logger.LogInformation("Hangfire: Starting AI analysis for claim {ClaimId}", claimId);

            var claim = await _context.Claims
                .Include(c => c.Images)
                .Include(c => c.Policy)
                .FirstOrDefaultAsync(c => c.Id == claimId && !c.IsDeleted);

            if (claim == null)
            {
                _logger.LogWarning("Hangfire: Claim {ClaimId} not found", claimId);
                return;
            }

            var imageUrls = claim.Images
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .ToList();

            if (imageUrls.Count == 0)
            {
                _logger.LogInformation("Hangfire: No images for claim {ClaimId}, recording error result", claimId);

                // Set explicit error result so admin/farmer see why AI didn't run
                claim.AIAnalysisResult = JsonSerializer.Serialize(new
                {
                    error = true,
                    damagePercentage = (double?)null,
                    damageDescription = "No damage photos uploaded — AI analysis requires at least one image.",
                    confidence = "N/A",
                    recommendation = "Upload damage photos to enable AI assessment"
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                claim.AIAnalysisUpdatedAt = DateTime.UtcNow;
                claim.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _notificationService.SendClaimUpdateAsync(claim.UserId, new ClaimNotificationDto
                {
                    ClaimId = claimId,
                    Status = claim.Status,
                    Title = "AI Analysis — Photos Required",
                    Message = "Your claim needs damage photos for AI analysis. Please upload at least one photo.",
                    NotificationType = "AIRequiresImages"
                });
                return;
            }

            // H10 FIX: Skip AI analysis if it ran recently AND no new images were uploaded.
            // Old logic used `imageCountAtLastAnalysis - wasAnalyzedWith` which was wrong —
            // it counted ALL images, not just those at last analysis time.
            if (claim.AIAnalysisUpdatedAt.HasValue
                && claim.AIAnalysisUpdatedAt.Value > DateTime.UtcNow.AddMinutes(-2))
            {
                // Count images created AFTER the last AI analysis — these are genuinely new
                var newImageCount = claim.Images.Count(i => !i.IsDeleted && i.CreatedAt > claim.AIAnalysisUpdatedAt.Value);

                if (newImageCount == 0)
                {
                    _logger.LogInformation(
                        "Hangfire: Claim {ClaimId} AI already ran at {LastRun}, no new images — skipping",
                        claimId, claim.AIAnalysisUpdatedAt.Value);
                    return;
                }

                _logger.LogInformation(
                    "Hangfire: Claim {ClaimId} has {NewCount} new images since last AI analysis — re-running",
                    claimId, newImageCount);
            }

            var cropType = claim.Policy?.CropType ?? claim.Farm?.CropType ?? "unknown";

            try
            {
                var aiResult = await _geminiService.AnalyzeImagesAsync(imageUrls, cropType);

                claim.AIAnalysisResult = JsonSerializer.Serialize(aiResult, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                claim.AIAnalysisUpdatedAt = DateTime.UtcNow;
                claim.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Hangfire: AI done for claim {ClaimId}. Damage={Damage}%, Confidence={Confidence}",
                    claimId, aiResult.DamagePercentage?.ToString() ?? "null", aiResult.Confidence);

                // M5: include claim status and notification message based on result
                var damageText = aiResult.DamagePercentage.HasValue
                    ? $"{aiResult.DamagePercentage}%"
                    : "Analysis could not be determined";

                await _notificationService.SendClaimUpdateAsync(claim.UserId, new ClaimNotificationDto
                {
                    ClaimId = claimId,
                    Status = claim.Status,
                    Title = "AI Damage Analysis Complete",
                    Message = $"Damage assessed: {damageText}. {aiResult.DamageDescription}",
                    NotificationType = "AIComplete",
                    AIDamagePercentage = aiResult.DamagePercentage,
                    AIConfidence = aiResult.Confidence
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hangfire: AI analysis failed for claim {ClaimId}", claimId);
                throw;
            }
        }
    }
}
