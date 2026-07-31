using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record AdminPolicyListDto
    {
        public Guid Id { get; init; }
        public Guid FarmId { get; init; }
        public Guid UserId { get; init; }
        public string FarmerName { get; init; } = string.Empty;
        public string FarmerEmail { get; init; } = string.Empty;
        public string FarmName { get; init; } = string.Empty;
        public string PolicyNumber { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string CropType { get; init; } = string.Empty;
        public decimal CoverageAmount { get; init; }
        public decimal Premium { get; init; }
        public decimal SumInsured { get; init; }
        public PolicyStatus Status { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public string? ApprovedByName { get; init; }
        public DateTime? RejectedAt { get; init; }
        public string? RejectionReason { get; init; }
        public int ClaimsCount { get; init; }
        public DateTime CreatedAt { get; init; }
        public string PaymentStatus { get; init; } = "Unpaid";

        // Installment tracking
        public int? CurrentInstallmentNumber { get; init; }
        public DateTime? NextInstallmentDueDate { get; init; }
        public decimal? InstallmentAmount { get; init; }
    }
}
