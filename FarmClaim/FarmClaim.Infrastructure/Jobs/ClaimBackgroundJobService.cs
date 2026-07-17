using System.Text.Json;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Notifications.DTOs;
using FarmClaim.Domain.Enums;
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
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 300 })]
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
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 300 })]
        public async Task ProcessAIAnalysisAsync(Guid claimId, List<string> imageUrls, string cropType)
        {
            _logger.LogInformation("Hangfire: Starting AI analysis for claim {ClaimId} with {Count} images",
                claimId, imageUrls.Count);

            var claim = await _context.Claims
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == claimId && !c.IsDeleted);

            if (claim == null)
            {
                _logger.LogWarning("Hangfire: Claim {ClaimId} not found", claimId);
                return;
            }

            if (claim.AIAnalysisResult != null)
            {
                _logger.LogInformation("Hangfire: Claim {ClaimId} already has AI analysis, skipping", claimId);
                return;
            }

            if (imageUrls.Count == 0)
            {
                _logger.LogInformation("Hangfire: No images for claim {ClaimId}, skipping AI", claimId);
                return;
            }

            try
            {
                var aiResult = await _geminiService.AnalyzeImagesAsync(imageUrls, cropType);

                claim.AIAnalysisResult = JsonSerializer.Serialize(aiResult, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                claim.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Hangfire: AI done for claim {ClaimId}. Damage={Damage}%, Confidence={Confidence}",
                    claimId, aiResult.DamagePercentage, aiResult.Confidence);

                // Notify farmer
                await _notificationService.SendClaimUpdateAsync(claim.UserId, new ClaimNotificationDto
                {
                    ClaimId = claimId,
                    Status = claim.Status,
                    Title = "AI Damage Analysis Complete",
                    Message = $"Damage assessed: {aiResult.DamagePercentage}%. {aiResult.DamageDescription}",
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