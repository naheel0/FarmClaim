using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public record PaymentResponseDto
    {
        public Guid Id { get; init; }
        public Guid PolicyId { get; init; }
        public string PolicyNumber { get; init; } = string.Empty;
        public Guid UserId { get; init; }
        public string OrderId { get; init; } = string.Empty;
        public string? PaymentId { get; init; }
        public decimal AmountInRupees { get; init; }
        public string Currency { get; init; } = "INR";
        public PaymentStatus Status { get; init; }
        public string? Method { get; init; }
        public string? MethodDescription { get; init; }
        public string? BankReference { get; init; }
        public string? FailureReason { get; init; }
        public decimal? Fee { get; init; }
        public decimal? Tax { get; init; }
        public DateTime? CapturedAt { get; init; }
        public DateTime? FailedAt { get; init; }
        public string? ReceiptNumber { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}