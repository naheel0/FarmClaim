using System;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.InsurancePolicies.DTOs
{
    public record PolicyListDto
    {
        public Guid Id { get; init; }
        public string PolicyNumber { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string CropType { get; init; } = string.Empty;
        public decimal CoverageAmount { get; init; }
        public decimal Premium { get; init; }
        public decimal SumInsured { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }

        // OLD: public bool IsActive { get; init; }
        // NEW:
        public PolicyStatus Status { get; init; }
        public string? RejectionReason { get; init; }

        public string? FarmName { get; init; }
        public Guid FarmId { get; init; }
        public int ClaimsCount { get; init; }

        // Installment tracking
        public int? CurrentInstallmentNumber { get; init; }
        public DateTime? NextInstallmentDueDate { get; init; }
        public decimal? InstallmentAmount { get; init; }
    }
}