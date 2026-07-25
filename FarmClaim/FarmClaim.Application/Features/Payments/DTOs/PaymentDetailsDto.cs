namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class PaymentDetailsDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string? Method { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardNetwork { get; set; }
        public string? Vpa { get; set; }
        public string? Bank { get; set; }
        public string? BankReference { get; set; }
        public string? Wallet { get; set; }
        public decimal? Fee { get; set; }
        public decimal? Tax { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}