namespace FarmClaim.Application.Features.Payments.DTOs
{
    public record RefundResultDto
    {
        public bool Success { get; init; }
        public string? RefundId { get; init; }
        public decimal AmountRefunded { get; init; }
        public string? Status { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
