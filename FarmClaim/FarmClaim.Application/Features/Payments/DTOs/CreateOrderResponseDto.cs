namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class CreateOrderResponseDto
    {
        public Guid PaymentId { get; set; }
        public Guid PolicyId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public long AmountInPaise { get; set; }
        public decimal AmountInRupees { get; set; }
        public string Currency { get; set; } = "INR";
        public string RazorpayKeyId { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = "Created";
        public CustomerInfo Customer { get; set; } = new();
    }

    public class CustomerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}