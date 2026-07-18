using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record ApprovePolicyResponseDto
    {
        public Guid Id { get; init; }
        public string PolicyNumber { get; init; } = string.Empty;
        public PolicyStatus Status { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public Guid? ApprovedByUserId { get; init; }
        public string? ApprovedByName { get; init; }
    }
}