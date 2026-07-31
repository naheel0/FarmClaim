using System;
using FarmClaim.Domain.Enums;

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

        // OLD: public bool IsActive { get; init; }
        // NEW:
        public PolicyStatus Status { get; init; }

        // NEW: Approval tracking
        public DateTime? ApprovedAt { get; init; }
        public Guid? ApprovedByUserId { get; init; }
        public string? ApprovedByName { get; init; }

        // NEW: Rejection tracking
        public DateTime? RejectedAt { get; init; }
        public string? RejectionReason { get; init; }

        // NEW: Cancellation tracking
        public DateTime? CancelledAt { get; init; }

        // Installment tracking
        public int? CurrentInstallmentNumber { get; init; }
        public DateTime? NextInstallmentDueDate { get; init; }
        public decimal? InstallmentAmount { get; init; }
        public List<PremiumScheduleDto>? PremiumSchedules { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int ClaimsCount { get; init; }
    }
}