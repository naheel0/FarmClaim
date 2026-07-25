namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class VerifyPaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? PaymentId { get; set; }
        public Guid? PolicyId { get; set; }
        public string? PolicyNumber { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime? CapturedAt { get; set; }
        public string? ReceiptNumber { get; set; }
    }
}