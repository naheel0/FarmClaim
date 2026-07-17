using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Notifications.DTOs
{
    public record ClaimNotificationDto
    {
        public Guid ClaimId { get; init; }
        public ClaimStatus Status { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string NotificationType { get; init; } = string.Empty; // WeatherComplete, AIComplete, StatusChanged
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        // Optional AI data
        public double? AIDamagePercentage { get; init; }
        public string? AIConfidence { get; init; }
        public decimal? ApprovedAmount { get; init; }
        public string? RejectionReason { get; init; }
    }
}