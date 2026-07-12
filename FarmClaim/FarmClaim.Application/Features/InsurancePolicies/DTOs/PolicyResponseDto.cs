using System;

namespace FarmClaim.Application.Features.InsurancePolicies.DTOs
{
    public record PolicyResponseDto
    {
        public Guid Id { get; init; }
        public Guid FarmId { get; init; }
        public Guid UserId { get; init; }
        public string FarmName { get; init; } = string.Empty;
        public string PolicyNumber { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string CropType { get; init; } = string.Empty;
        public decimal CoverageAmount { get; init; }
        public decimal Premium { get; init; }
        public decimal SumInsured { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int ClaimsCount { get; init; }
    }
}