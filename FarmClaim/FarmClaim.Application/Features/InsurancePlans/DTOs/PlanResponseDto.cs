using System;

namespace FarmClaim.Application.Features.InsurancePlans.DTOs
{
    public record PlanResponseDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string CropType { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public decimal PremiumRatePerHectare { get; init; }
        public decimal SumInsuredPerHectare { get; init; }
        public decimal CoveragePercentage { get; init; }
        public decimal? MinAreaInHectares { get; init; }
        public decimal? MaxAreaInHectares { get; init; }
        public int PolicyDurationMonths { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int PoliciesCount { get; init; }
    }
}