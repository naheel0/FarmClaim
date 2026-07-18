using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record AdminClaimDetailDto
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
        public decimal CoverageAmount { get; init; }
        public double? FarmLatitude { get; init; }
        public double? FarmLongitude { get; init; }
        public DateTime IncidentDate { get; init; }
        public IncidentType IncidentType { get; init; }
        public string? Description { get; init; }
        public string? DamageDescription { get; init; }
        public ClaimStatus Status { get; init; }
        public decimal? ApprovedAmount { get; init; }

        // Review tracking
        public string? ReviewedBy { get; init; }
        public DateTime? ReviewedAt { get; init; }
        public string? RejectionReason { get; init; }

        // NEW: Admin user who reviewed
        public Guid? ReviewedByUserId { get; init; }
        public string? ReviewedByName { get; init; }

        // NEW: Payment tracking
        public DateTime? PaidAt { get; init; }
        public string? PaymentReference { get; init; }

        // AI & Weather (unchanged)
        public string? WeatherSnapshot { get; init; }
        public string? AIAnalysisResult { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }

        // AI Recommendation (unchanged)
        public string? AIRecommendation { get; init; }
        public decimal? SuggestedPayout { get; init; }
        public double? AIDamagePercentage { get; init; }
        public string? AIConfidence { get; init; }

        public List<ClaimImageDto> Images { get; init; } = new();
    }
}