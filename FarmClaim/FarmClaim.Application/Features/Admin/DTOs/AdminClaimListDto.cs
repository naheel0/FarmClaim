using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record AdminClaimListDto
    {
        public Guid Id { get; init; }
        public Guid PolicyId { get; init; }
        public Guid FarmId { get; init; }
        public Guid UserId { get; init; }
        public string FarmerName { get; init; } = string.Empty;
        public string FarmerEmail { get; init; } = string.Empty;
        public string PolicyNumber { get; init; } = string.Empty;
        public string FarmName { get; init; } = string.Empty;
        public string CropType { get; init; } = string.Empty;
        public decimal SumInsured { get; init; }
        public DateTime IncidentDate { get; init; }
        public IncidentType IncidentType { get; init; }
        public ClaimStatus Status { get; init; }
        public decimal? ApprovedAmount { get; init; }
        public int ImageCount { get; init; }
        public bool HasAIAnalysis { get; init; }
        public bool HasWeatherData { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}